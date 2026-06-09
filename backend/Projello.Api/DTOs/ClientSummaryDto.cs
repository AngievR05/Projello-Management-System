namespace Projello.Api.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;



    [ExcludeFromCodeCoverage]
    public class ClientSummaryDto
    {
        //Null if not available
        public decimal? TotalRevenue { get; set; }
        public decimal? Outstanding { get; set; }

        public int ActiveClients { get; set; }
        public int BlacklistClients { get; set; }
        
    }
