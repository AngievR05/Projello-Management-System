using System.Diagnostics.CodeAnalysis;
namespace Projello.Api.DTOs
{
    [ExcludeFromCodeCoverage]
    public class UserUpdateDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string AvatarSeed { get; set; } = string.Empty;
        public string AvatarBackground { get; set; } = string.Empty;
    }
}