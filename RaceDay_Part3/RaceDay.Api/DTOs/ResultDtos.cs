using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultCreateDto
{
    /// <summary>Format "hh:mm:ss", e.g. "00:52:14".</summary>
    [Required]
    public string FinishTime { get; set; } = string.Empty;

    [Required]
    public int FinishPosition { get; set; }
}

public class ResultResponseDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string FinishTime { get; set; } = string.Empty;
    public int FinishPosition { get; set; }
}
