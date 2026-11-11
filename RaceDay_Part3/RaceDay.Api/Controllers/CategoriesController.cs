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

    /// <summary>Updates a category of an event owned by the logged-in Organiser.</summary>
    [HttpPut("{categoryId}")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<CategoryResponseDto>> Update(int eventId, int categoryId, [FromBody] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.EventId == eventId);
        if (category is null || category.Event is null)
            return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserId != CurrentUserId) return Forbid();

        category.CategoryName = dto.CategoryName;
        category.MinAge = dto.MinAge;
        category.MaxAge = dto.MaxAge;
        category.CategoryDistance = dto.CategoryDistance;

        await _db.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>Deletes a category of an event owned by the logged-in Organiser (only if nobody enrolled in it).</summary>
    [HttpDelete("{categoryId}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int eventId, int categoryId)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.EventId == eventId);
        if (category is null || category.Event is null)
            return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserId != CurrentUserId) return Forbid();

        var hasEnrolments = await _db.EventEnrolments.AnyAsync(e => e.CategoryId == categoryId);
        if (hasEnrolments)
            return Conflict(new { message = "This category already has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
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
