using System.Diagnostics.CodeAnalysis;

namespace Projello.Api.DTOs
{
    [ExcludeFromCodeCoverage]
    public class MilestoneUpdateDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly? DueDate { get; set; }
        public string Status { get; set; } = "NotStarted";
        public DateOnly? CompletedDate { get; set; }

        public int? Progress { get; set; }
    }
}