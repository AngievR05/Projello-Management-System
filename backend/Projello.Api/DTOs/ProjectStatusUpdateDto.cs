using System.Diagnostics.CodeAnalysis;

namespace Projello.Api.DTOs
{
    [ExcludeFromCodeCoverage]
    public class ProjectStatusUpdateDto
    {
        public string Status { get; set; } = null!;
    }
}