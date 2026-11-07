using System.Globalization;
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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Captures the finish time and position for a Participant's enrolment.</summary>
    [HttpPost("api/enrolments/{enrolmentId}/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<ResultResponseDto>> Create(int enrolmentId, [FromBody] ResultCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var enrolment = await _db.EventEnrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null || enrolment.Event is null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event.OrganiserId != CurrentUserId) return Forbid();

        var alreadyHasResult = await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId);
        if (alreadyHasResult)
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        if (!TimeSpan.TryParseExact(dto.FinishTime, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var finishTime))
            return BadRequest(new { message = "FinishTime must be in the format hh:mm:ss." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = finishTime,
            FinishPosition = dto.FinishPosition
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForEvent), new { eventId = enrolment.Event.EventId }, ToDto(result));
    }

    /// <summary>Returns the logged-in Participant's own results across all events.</summary>
    [HttpGet("api/users/me/results")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetMine()
    {
        var results = await _db.Results
            .Include(r => r.Enrolment)
            .Where(r => r.Enrolment!.ParticipantId == CurrentUserId)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Returns all results for an event, visible to the owning Organiser.</summary>
    [HttpGet("api/events/{eventId}/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var results = await _db.Results
            .Include(r => r.Enrolment)
            .Where(r => r.Enrolment!.EventId == eventId)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Ok(results);
    }

    private static ResultResponseDto ToDto(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        FinishTime = r.FinishTime.ToString(@"hh\:mm\:ss"),
        FinishPosition = r.FinishPosition
    };
}
