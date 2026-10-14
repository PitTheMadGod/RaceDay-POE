using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null) return NotFound();

        return Ok(new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.RoleName
        });
    }

    /// <summary>Updates the profile of the currently logged-in user.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return NotFound();

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile updated." });
    }
}
