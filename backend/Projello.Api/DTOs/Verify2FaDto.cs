using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Projello.Api.DTOs;

[ExcludeFromCodeCoverage]
public class Verify2FaDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;
}