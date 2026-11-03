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
    public class GeminiProvider : IAiProvider
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(25) };

        public string ProviderName => "Gemini";

        public async Task<(string answer, int latencyMs)> GenerateResponseAsync(string systemPrompt, string userPrompt, AppConfig config, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException("Chưa cấu hình API Key cho Google Gemini. Vui lòng vào Cài Đặt để nhập API Key.");
            }

            var modelName = !string.IsNullOrWhiteSpace(config.ModelName) ? config.ModelName : "gemini-1.5-flash";
            var requestUrl = !string.IsNullOrWhiteSpace(config.BaseUrl)
                ? $"{config.BaseUrl.TrimEnd('/')}/models/{modelName}:generateContent?key={config.ApiKey.Trim()}"
                : $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={config.ApiKey.Trim()}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new object[]
                    {
                        new { text = systemPrompt }
                    }
                },
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = userPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.5,
                    maxOutputTokens = 800
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var sw = Stopwatch.StartNew();
            using var response = await _httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Lỗi API Gemini ({response.StatusCode}): {responseContent}");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCand = candidates[0];
                    if (firstCand.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        return (text?.Trim() ?? string.Empty, (int)sw.ElapsedMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi phân tích phản hồi Gemini: {ex.Message}");
            }

            return ("(Không trích xuất được phản hồi từ Gemini)", (int)sw.ElapsedMilliseconds);
        }
    }
}

