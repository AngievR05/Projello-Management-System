using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Projello.Api.DTOs
{
    [ExcludeFromCodeCoverage]
    public class ReactionCreateDto
    {
        [Required]
        public string Emoji { get; set; } = null!; // e.g., "👍"
    }
}