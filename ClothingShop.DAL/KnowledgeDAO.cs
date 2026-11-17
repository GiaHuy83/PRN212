using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClothingShop.DAL.Models;
using Npgsql;

namespace ClothingShop.DAL
{
    public class KnowledgeDAO
    {
        private readonly string _connectionString;

        public KnowledgeDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<string?> SearchKnowledgeAsync(string query, int limit = 3, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            if (!DatabaseConnection.IsConfigured(_connectionString))
            {
                throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối Supabase PostgreSQL để tra cứu RAG.");
            }

            await using var conn = DatabaseConnection.CreateConnection(_connectionString);
            await conn.OpenAsync(ct);

            const string sql = "SELECT title, content FROM public.search_knowledge_documents_text(@q, @limit);";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", query.Trim());
            cmd.Parameters.AddWithValue("@limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var sb = new StringBuilder();
            while (await reader.ReadAsync(ct))
            {
                var title = reader.GetString(0);
                var content = reader.GetString(1);
                sb.AppendLine($"[TÀI LIỆU CỬA HÀNG]: {title}");
                sb.AppendLine(content);
                sb.AppendLine();
            }

            return sb.Length > 0 ? sb.ToString().Trim() : null;
        }
    }
}
