namespace Projello.Api.DTOs;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
    public class ClientUpdateDto
    {
        [Range(0, double.MaxValue)]
        public decimal? TotalPaid { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Outstanding { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }
    }