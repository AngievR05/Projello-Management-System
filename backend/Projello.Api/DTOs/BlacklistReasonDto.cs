namespace Projello.Api.DTOs;
using System.Diagnostics.CodeAnalysis;


    // Sent by admin when blacklisting a client
    [ExcludeFromCodeCoverage]
    public class BlacklistClientDto
    {
        public string? Reason { get; set; }
    }
