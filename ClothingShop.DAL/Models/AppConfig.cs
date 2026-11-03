using System;

namespace ClothingShop.DAL.Models
{
    public class AppConfig
    {
        // Nhà cung cấp AI thật: Gemini, OpenAI, Claude (Bắt buộc gọi API thật 100%, không dùng Mock)
        public string Provider { get; set; } = "Gemini"; 
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "gemini-1.5-flash";
        public string BaseUrl { get; set; } = string.Empty;

        // Cấu hình Supabase PostgreSQL
        public string SupabaseConnectionString { get; set; } = "Host=db.[YOUR-PROJECT-REF].supabase.co;Port=5432;Database=postgres;Username=postgres;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;";
    }
}
