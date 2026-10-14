using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists all events. Visible to any logged-in user.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetAll()
    {
        var events = await _db.Events.Select(e => ToDto(e)).ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns the details of a single event.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponseDto>> GetById(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        return Ok(ToDto(ev));
    }

    /// <summary>Creates a new event. Organiser only.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<EventResponseDto>> Create([FromBody] EventCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.EventType is not ("Run" or "Walk" or "Cycle"))
            return BadRequest(new { message = "EventType must be 'Run', 'Walk', or 'Cycle'." });

        var ev = new Event
        {
            Name = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            Distance = dto.Distance,
            EventType = dto.EventType,
            OrganiserId = CurrentUserId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToDto(ev));
    }

    /// <summary>Updates an event owned by the logged-in Organiser.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, [FromBody] EventCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId)
            return Forbid();

        if (dto.EventType is not ("Run" or "Walk" or "Cycle"))
            return BadRequest(new { message = "EventType must be 'Run', 'Walk', or 'Cycle'." });

        ev.Name = dto.Name;
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType;

        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>Deletes an event owned by the logged-in Organiser.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId)
            return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static EventResponseDto ToDto(Event e) => new()
    {
        EventId = e.EventId,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        OrganiserId = e.OrganiserId
    };
}
