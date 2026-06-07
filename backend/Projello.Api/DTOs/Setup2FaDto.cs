using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Projello.Api.DTOs;

[ExcludeFromCodeCoverage]
public class Setup2FaDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}