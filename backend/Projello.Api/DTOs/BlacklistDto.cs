namespace Projello.Api.DTOs;
using System.Diagnostics.CodeAnalysis;


    // Returned in API responses for client blacklist status
    [ExcludeFromCodeCoverage]
    public class ClientBlacklistStatusDto
    {
        public int ClientID { get; set; }
        public string Name { get; set; } = null!;
        public bool IsBlacklisted { get; set; }
        public string? BlacklistReason { get; set; }
        public DateTime? BlacklistedAt { get; set; }
        public string? BlacklistedByName { get; set; }
    }

 