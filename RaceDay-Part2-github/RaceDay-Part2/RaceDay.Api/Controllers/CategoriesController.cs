using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId}/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists all categories available for an event.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Defines a new category for an event owned by the logged-in Organiser.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<CategoryResponseDto>> Create(int eventId, [FromBody] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName,
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            CategoryDistance = dto.CategoryDistance
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { eventId }, ToDto(category));
    }

    private static CategoryResponseDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        CategoryDistance = c.CategoryDistance
    };
}
