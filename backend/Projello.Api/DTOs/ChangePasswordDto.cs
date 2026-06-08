namespace Projello.Api.DTOs;
using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
