using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

    namespace Projello.Api.DTOs;
    [ExcludeFromCodeCoverage]
    public class UserLoginDto
    {
        // Validates email format before hitting the database
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;


        [Required]
        public string Password { get; set; } = null!;
    }
