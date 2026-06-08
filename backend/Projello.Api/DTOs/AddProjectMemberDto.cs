namespace Projello.Api.DTOs;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class AddProjectMemberDto
{
    public string UserID { get; set; } = string.Empty;
    public string? AssignedAs { get; set; }  // "Worker", "Foreman", etc.
}