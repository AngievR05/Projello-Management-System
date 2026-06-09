using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Projello.Api.DTOs
{
    [ExcludeFromCodeCoverage]
    public class ProgressUpdateCreateDto
    {
        [MaxLength(500)]
        public string? OptionalComment { get; set; }
    }
}