using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
namespace Projello.Api.DTOs
{
    [ExcludeFromCodeCoverage]
    public class MilestoneCreateDto
    {
        [Required]
        public int ProjectID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateOnly? DueDate { get; set; }


        public int Progress { get; set; } = 0;
    }
}