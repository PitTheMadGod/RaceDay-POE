using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryCreateDto
{
    [Required]
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? CategoryDistance { get; set; }
}

public class CategoryResponseDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? CategoryDistance { get; set; }
}
