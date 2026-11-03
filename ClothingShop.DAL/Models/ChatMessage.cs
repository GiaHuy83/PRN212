using System;
using System.Collections.Generic;

namespace ClothingShop.DAL.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Role { get; set; } = "user"; // "user" hoặc "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRag { get; set; }
        public string Route { get; set; } = "general_chat";
        public int LatencyMs { get; set; }
        public List<ClothingProduct> ReferencedProducts { get; set; } = new();

        public bool IsUser => Role == "user";
        public bool IsAssistant => Role == "assistant";
        public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm:ss");

        public string RouteBadgeText => Route switch
        {
            "clothing_recommendation" => "Gợi Ý Quần Áo (RAG)",
            "size_consultation" => "Tư Vấn Size (RAG)",
            "store_policy" => "Chính Sách & Đổi Trả (RAG)",
            "chitchat" => "Chào Hỏi (Fast Path)",
            "out_of_scope" => "Ngoài Phạm Vi",
            "security_threat" => "Chặn Bảo Mật",
            "unknown" => "Không Có Dữ Liệu",
            _ => IsRag ? "AI RAG Có Dữ Liệu" : "Chat LLM Ra Ngoài"
        };
    }
}
