using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Enrols the logged-in Participant into an event under a chosen category.</summary>
    [HttpPost("api/events/{eventId}/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<EnrolmentResponseDto>> Enrol(int eventId, [FromBody] EnrolmentCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == eventId);
        if (category is null)
            return BadRequest(new { message = "That category does not belong to this event." });

        var alreadyEnrolled = await _db.EventEnrolments
            .AnyAsync(e => e.EventId == eventId && e.ParticipantId == CurrentUserId);
        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new EventEnrolment
        {
            EventId = eventId,
            CategoryId = dto.CategoryId,
            ParticipantId = CurrentUserId,
            EnrolmentDate = DateTime.UtcNow
        };

        _db.EventEnrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMine), null, ToDto(enrolment));
    }

    /// <summary>Lists all enrolments for an event, visible to the owning Organiser.</summary>
    [HttpGet("api/events/{eventId}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<IEnumerable<EnrolmentResponseDto>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var enrolments = await _db.EventEnrolments
            .Where(e => e.EventId == eventId)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    [HttpGet("api/users/me/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<IEnumerable<EnrolmentResponseDto>>> GetMine()
    {
        var enrolments = await _db.EventEnrolments
            .Where(e => e.ParticipantId == CurrentUserId)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(enrolments);
    }

    private static EnrolmentResponseDto ToDto(EventEnrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        EventId = e.EventId,
        CategoryId = e.CategoryId,
        ParticipantId = e.ParticipantId,
        EnrolmentDate = e.EnrolmentDate
    };
}
