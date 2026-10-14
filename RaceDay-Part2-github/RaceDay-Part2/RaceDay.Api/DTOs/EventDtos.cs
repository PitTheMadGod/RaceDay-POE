using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventCreateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    public decimal Distance { get; set; }

    /// <summary>"Run", "Walk", or "Cycle"</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class EventResponseDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
}
