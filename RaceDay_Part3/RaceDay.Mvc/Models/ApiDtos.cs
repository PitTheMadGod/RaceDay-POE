namespace RaceDay.Mvc.Models;

// These mirror the DTOs returned by the Part 2 API's JSON responses.
// Kept in one file since the MVC project only ever reads/writes them as plain data.

public class UserProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public string? BannerImageUrl { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? CategoryDistance { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public DateTime EnrolmentDate { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string FinishTime { get; set; } = string.Empty;
    public int FinishPosition { get; set; }
}

public class UploadResultDto
{
    public string Url { get; set; } = string.Empty;
}

public class ApiErrorDto
{
    public string? Message { get; set; }
}
