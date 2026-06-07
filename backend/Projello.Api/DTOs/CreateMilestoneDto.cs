namespace Projello.Api.DTOs;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;


[ExcludeFromCodeCoverage]
public class CreateMilestoneDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public string Status { get; set; } = "Pending";
}