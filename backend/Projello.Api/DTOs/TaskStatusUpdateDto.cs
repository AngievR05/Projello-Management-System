using System.Diagnostics.CodeAnalysis;

namespace Projello.Api.DTOs
{
    [ExcludeFromCodeCoverage]
    public class TaskStatusUpdateDto
    {
        // Acceptable values: NotStarted, InProgress, Completed, Blocked
        public string Status { get; set; } = null!;
    }
}