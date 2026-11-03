using System.Collections.Generic;

namespace ClothingShop.DAL.Models
{
    public class RouteDecision
    {
        public string Route { get; set; } = "general_chat";
        public double Confidence { get; set; } = 1.0;
        public string? SuggestedReply { get; set; }
        public string? SuggestedAction { get; set; }
        public bool IsBlocked { get; set; }
        public Dictionary<string, string> Entities { get; set; } = new();
        public double LatencyMs { get; set; }
    }
}

