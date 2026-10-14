using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EnrolmentCreateDto
{
    [Required]
    public int CategoryId { get; set; }
}

public class EnrolmentResponseDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public DateTime EnrolmentDate { get; set; }
}
