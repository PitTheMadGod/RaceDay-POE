using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Registers a new user account and assigns their chosen role.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });

        var emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
            return Conflict(new { message = "A user with this email is already registered." });

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.Role);
        if (role is null)
            return BadRequest(new { message = "Role not found." });

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            RoleId = role.RoleId,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created($"/api/users/{user.UserId}", new
        {
            userId = user.UserId,
            role = role.RoleName
        });
    }

    /// <summary>Authenticates a user and starts a session via an auth cookie.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null || user.Role is null)
            return Unauthorized(new { message = "Invalid email or password." });

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.RoleName)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { userId = user.UserId, fullName = user.FullName, role = user.Role.RoleName });
    }

    /// <summary>Ends the current user's session.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out." });
    }
}
