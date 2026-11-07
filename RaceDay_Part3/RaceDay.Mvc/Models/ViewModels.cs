using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class RegisterViewModel
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Participant";

    public string? ErrorMessage { get; set; }
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.1, 1000)]
    public decimal Distance { get; set; }

    [Required]
    public string EventType { get; set; } = "Run";

    public string? ExistingBannerImageUrl { get; set; }

    public IFormFile? BannerImage { get; set; }

    public string? ErrorMessage { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required]
    public string CategoryName { get; set; } = string.Empty;

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? CategoryDistance { get; set; }

    public string? ErrorMessage { get; set; }
}

public class ProfileViewModel
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public string? ExistingProfilePictureUrl { get; set; }
    public IFormFile? ProfilePicture { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class ResultFormViewModel
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Finish time (hh:mm:ss)")]
    public string FinishTime { get; set; } = string.Empty;

    [Required]
    public int FinishPosition { get; set; }

    public string? ErrorMessage { get; set; }
}

/// <summary>A joined row for the Organiser's "view enrolments for this event" page.</summary>
public class EnrolmentRowViewModel
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public bool HasResult { get; set; }
}

/// <summary>A joined row for the Participant's "My Results" page.</summary>
public class MyResultRowViewModel
{
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int FinishPosition { get; set; }
}

/// <summary>A joined row for the Participant's "My Enrolments" page.</summary>
public class MyEnrolmentRowViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = "Confirmed"; // Confirmed once saved by the API; no pending state in this model
}
