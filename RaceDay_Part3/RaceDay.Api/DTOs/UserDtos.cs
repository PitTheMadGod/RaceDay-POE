namespace RaceDay.Api.DTOs;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>URL returned by POST /api/files/upload. Optional — leave null to keep the current picture.</summary>
    public string? ProfilePictureUrl { get; set; }
}
