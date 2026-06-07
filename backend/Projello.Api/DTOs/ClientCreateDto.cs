namespace Projello.Api.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class ClientCreateDto
    {
        [Required(ErrorMessage = "Client name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Company { get; set; }

        [MaxLength(255)]
        public string? Notes { get; set; }
    }
