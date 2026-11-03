using System;

namespace ClothingShop.DAL.Models
{
    public class KnowledgeDocument
    {
        public long Id { get; set; }
        public string Category { get; set; } = "general";
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

