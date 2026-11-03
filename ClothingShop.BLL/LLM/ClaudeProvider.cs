using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClothingShop.DAL.Models;

namespace ClothingShop.BLL.LLM
{
    public class ClaudeProvider : IAiProvider
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(25) };
        private const string DefaultClaudeBaseUrl = "https://api.anthropic.com/v1/messages";

        public string ProviderName => "Claude";

        public async Task<(string answer, int latencyMs)> GenerateResponseAsync(string systemPrompt, string userPrompt, AppConfig config, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException("Chưa cấu hình API Key cho Anthropic Claude. Vui lòng vào Cài Đặt để nhập API Key.");
            }

            var baseUrl = !string.IsNullOrWhiteSpace(config.BaseUrl) ? config.BaseUrl : DefaultClaudeBaseUrl;
            var modelName = !string.IsNullOrWhiteSpace(config.ModelName) ? config.ModelName : "claude-3-5-sonnet-20241022";

            var requestBody = new
            {
                model = modelName,
                max_tokens = 800,
                system = systemPrompt,
                messages = new object[]
                {
                    new { role = "user", content = userPrompt }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("x-api-key", config.ApiKey.Trim());
            request.Headers.Add("anthropic-version", "2023-06-01");

            var sw = Stopwatch.StartNew();
            using var response = await _httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Lỗi API Claude ({response.StatusCode}): {responseContent}");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var item in contentArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("text", out var textProp))
                        {
                            sb.Append(textProp.GetString());
                        }
                    }
                    var result = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(result))
                    {
                        return (result, (int)sw.ElapsedMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi phân tích phản hồi Claude: {ex.Message}");
            }

            return ("(Không trích xuất được phản hồi từ Claude)", (int)sw.ElapsedMilliseconds);
        }
    }
}

