using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClothingShop.DAL.Models;

namespace ClothingShop.BLL.LLM
{
    public class OpenAiProvider : IAiProvider
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(25) };
        private const string DefaultOpenAiBaseUrl = "https://api.openai.com/v1/chat/completions";

        public string ProviderName => "OpenAI";

        public async Task<(string answer, int latencyMs)> GenerateResponseAsync(string systemPrompt, string userPrompt, AppConfig config, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException("Chưa cấu hình API Key cho OpenAI. Vui lòng vào Cài Đặt để nhập API Key.");
            }

            var baseUrl = !string.IsNullOrWhiteSpace(config.BaseUrl) ? config.BaseUrl : DefaultOpenAiBaseUrl;
            var modelName = !string.IsNullOrWhiteSpace(config.ModelName) ? config.ModelName : "gpt-4o-mini";

            var requestBody = new
            {
                model = modelName,
                temperature = 0.5,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey.Trim());

            var sw = Stopwatch.StartNew();
            using var response = await _httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Lỗi API OpenAI ({response.StatusCode}): {responseContent}");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var contentProp))
                    {
                        var result = contentProp.GetString()?.Trim() ?? string.Empty;
                        return (result, (int)sw.ElapsedMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi phân tích phản hồi OpenAI: {ex.Message}");
            }

            return ("(Không trích xuất được phản hồi từ OpenAI)", (int)sw.ElapsedMilliseconds);
        }
    }
}

