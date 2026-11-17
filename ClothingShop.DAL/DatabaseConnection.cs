using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace ClothingShop.DAL
{
    public static class DatabaseConnection
    {
        public static bool IsConfigured(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return false;

            return !connectionString.Contains("[YOUR-PASSWORD]", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("[YOUR-PROJECT-REF]", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeConnectionString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var trimmed = input.Trim();

            // 1. Trường hợp người dùng dán dạng URI (postgresql://user:pass@host:port/db)
            if (trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(trimmed);
                    var userInfo = uri.UserInfo.Split(':', 2);
                    var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
                    var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
                    var host = uri.Host;
                    var port = uri.Port > 0 ? uri.Port : 5432;
                    var db = uri.AbsolutePath.TrimStart('/');
                    if (string.IsNullOrWhiteSpace(db)) db = "postgres";

                    return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;";
                }
                catch
                {
                    // Fallback khi mật khẩu chứa ký tự đặc biệt chưa URL-encode (như @, #, ?)
                    var schemeIdx = trimmed.IndexOf("://", StringComparison.Ordinal);
                    var afterScheme = schemeIdx >= 0 ? trimmed.Substring(schemeIdx + 3) : trimmed;
                    var atIdx = afterScheme.LastIndexOf('@');
                    if (atIdx > 0)
                    {
                        var creds = afterScheme.Substring(0, atIdx);
                        var hostPart = afterScheme.Substring(atIdx + 1);
                        var colonCredIdx = creds.IndexOf(':');
                        var user = colonCredIdx >= 0 ? Uri.UnescapeDataString(creds.Substring(0, colonCredIdx)) : Uri.UnescapeDataString(creds);
                        var pass = colonCredIdx >= 0 ? Uri.UnescapeDataString(creds.Substring(colonCredIdx + 1)) : string.Empty;

                        var slashIdx = hostPart.IndexOf('/');
                        var hostPort = slashIdx >= 0 ? hostPart.Substring(0, slashIdx) : hostPart;
                        var db = slashIdx >= 0 ? hostPart.Substring(slashIdx + 1).Split('?')[0] : "postgres";
                        if (string.IsNullOrWhiteSpace(db)) db = "postgres";

                        var colonHostIdx = hostPort.LastIndexOf(':');
                        var host = colonHostIdx >= 0 ? hostPort.Substring(0, colonHostIdx) : hostPort;
                        var port = colonHostIdx >= 0 && int.TryParse(hostPort.Substring(colonHostIdx + 1), out var p) ? p : 5432;

                        return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;";
                    }
                }
            }

            // 2. Trường hợp dạng space-separated: host=... port=... dbname=... user=... password=...
            if (trimmed.Contains("host=") && !trimmed.Contains(";"))
            {
                var parts = trimmed.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                foreach (var part in parts)
                {
                    if (part.StartsWith("dbname=", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append($"Database={part.Substring(7)};");
                    }
                    else if (part.StartsWith("user=", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append($"Username={part.Substring(5)};");
                    }
                    else if (part.StartsWith("host=", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append($"Host={part.Substring(5)};");
                    }
                    else if (part.StartsWith("port=", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append($"Port={part.Substring(5)};");
                    }
                    else if (part.StartsWith("password=", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append($"Password={part.Substring(9)};");
                    }
                    else if (part.StartsWith("sslmode=", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append($"SSL Mode=Require;");
                    }
                    else
                    {
                        sb.Append($"{part};");
                    }
                }
                sb.Append("Trust Server Certificate=true;");
                return sb.ToString();
            }

            // 3. Trường hợp chuẩn hoặc đã có dấu chấm phẩy
            if (!trimmed.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true;";
            }

            return trimmed;
        }

        public static async Task<(bool success, string message)> TestConnectionAsync(string connectionString, CancellationToken ct = default)
        {
            if (!IsConfigured(connectionString))
            {
                return (false, "Chuỗi kết nối chưa được cấu hình (đang rỗng hoặc còn placeholder [YOUR-PASSWORD] / [YOUR-PROJECT-REF]).");
            }

            try
            {
                var normalized = NormalizeConnectionString(connectionString);
                var csb = new NpgsqlConnectionStringBuilder(normalized)
                {
                    Timeout = 8,
                    CommandTimeout = 8
                };

                await using var conn = new NpgsqlConnection(csb.ConnectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new NpgsqlCommand("SELECT version();", conn);
                var version = await cmd.ExecuteScalarAsync(ct);

                return (true, $"Kết nối Supabase thành công!\nPhiên bản CSDL: {version}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối Supabase: {ex.Message}");
            }
        }

        public static NpgsqlConnection CreateConnection(string connectionString)
        {
            var normalized = NormalizeConnectionString(connectionString);
            var csb = new NpgsqlConnectionStringBuilder(normalized)
            {
                Timeout = 8,
                CommandTimeout = 8
            };
            return new NpgsqlConnection(csb.ConnectionString);
        }
    }
}
