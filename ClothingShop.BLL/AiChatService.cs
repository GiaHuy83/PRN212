using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClothingShop.BLL.LLM;
using ClothingShop.DAL;
using ClothingShop.DAL.Models;

namespace ClothingShop.BLL
{
    public class AiChatService
    {
        private ProductDAO _productDao;
        private KnowledgeDAO _knowledgeDao;
        private AppConfig _config;

        public AiChatService(string connectionString, AppConfig config)
        {
            _productDao = new ProductDAO(connectionString);
            _knowledgeDao = new KnowledgeDAO(connectionString);
            _config = config;
        }

        public void UpdateConfig(AppConfig config)
        {
            _config = config;
            _productDao = new ProductDAO(config.SupabaseConnectionString);
            _knowledgeDao = new KnowledgeDAO(config.SupabaseConnectionString);
        }

        public async Task<ChatMessage> SendMessageAsync(string userInput, bool isRagMode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = "Bạn vui lòng nhập nội dung câu hỏi để shop có thể hỗ trợ nhé!",
                    IsRag = isRagMode,
                    Route = "general_chat",
                    LatencyMs = 0
                };
            }

            var cleanInput = userInput.Trim();

            // 1. CHẾ ĐỘ CHAT RA NGOÀI (GENERAL CHAT - GỌI TRỰC TIẾP API LLM THẬT)
            if (!isRagMode)
            {
                return await HandleGeneralChatAsync(cleanInput, ct);
            }

            // 2. CHẾ ĐỘ CHAT VỚI AI (RAG - TRUY XUẤT DỮ LIỆU KHO HÀNG & GỌI API LLM THẬT)
            return await HandleRagChatAsync(cleanInput, ct);
        }

        private async Task<ChatMessage> HandleGeneralChatAsync(string input, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = $"⚠️ Bạn chưa nhập API Key cho {_config.Provider}.\n\nVui lòng chuyển sang Tab 'Cấu Hình API Key & CSDL' để nhập API Key của {_config.Provider} trước khi bắt đầu chat.",
                    IsRag = false,
                    Route = "general_chat",
                    LatencyMs = 0
                };
            }

            var systemPrompt = @"Bạn là trợ lý AI chuyên về thời trang (Fashion Stylist AI).
QUY TẮC TRẢ LỜI:
1. Trả lời bằng tiếng Việt lịch sự, thân thiện, súc tích và có tính thẩm mỹ cao.
2. Cung cấp kiến thức chuyên sâu về xu hướng thời trang, phối màu, mix & match trang phục, phong cách (Streetwear, Minimalism, Smart Casual...).
3. Sử dụng Markdown rõ ràng: dùng gạch đầu dòng (-) và in đậm (**...**) các điểm nhấn quan trọng.";

            var provider = GetProvider(_config.Provider);
            try
            {
                var (answer, latency) = await provider.GenerateResponseAsync(systemPrompt, input, _config, ct);
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = answer,
                    IsRag = false,
                    Route = "general_chat",
                    LatencyMs = latency
                };
            }
            catch (Exception ex)
            {
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = $"❌ Lỗi kết nối đến API {_config.Provider}: {ex.Message}\n\nVui lòng kiểm tra lại tính hợp lệ của API Key, kết nối mạng Internet hoặc hạn mức tài khoản.",
                    IsRag = false,
                    Route = "general_chat",
                    LatencyMs = 100
                };
            }
        }

        private async Task<ChatMessage> HandleRagChatAsync(string input, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            // BƯỚC 1: TIẾP NHẬN & PHÂN LOẠI GUARDRAILS BẢO MẬT
            var decision = EvaluateGuardrailsAndIntent(input);

            // Chặn prompt injection hoặc các yêu cầu độc hại
            if (decision.IsBlocked || decision.Route == "security_threat")
            {
                sw.Stop();
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = decision.SuggestedReply ?? "Yêu cầu bị từ chối do vi phạm chính sách bảo mật hệ thống.",
                    IsRag = true,
                    Route = decision.Route,
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }

            // BƯỚC 2: TRUY XUẤT DỮ LIỆU SẢN PHẨM & TÀI LIỆU KIẾN THỨC (RETRIEVAL PHASE)
            List<ClothingProduct> matchedProducts;
            string? knowledgeContext;
            try
            {
                matchedProducts = await RetrieveMatchingProductsAsync(input, ct);
                knowledgeContext = await _knowledgeDao.SearchKnowledgeAsync(input, 3, ct);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = $"⚠️ Không thể truy xuất dữ liệu RAG từ Supabase:\n{ex.Message}\n\nVui lòng kiểm tra lại Connection String trong Tab 'Cấu Hình API Key AI & CSDL'.",
                    IsRag = true,
                    Route = decision.Route,
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }

            // BƯỚC 3: KIỂM TRA ĐỦ DỮ LIỆU CSDL
            // Nếu hỏi về sản phẩm, size, chính sách nhưng trong kho và tài liệu không có dữ liệu -> Báo không có thông tin
            bool isProductOrSizeQuery = input.Contains("áo") || input.Contains("quần") || input.Contains("váy") ||
                                       input.Contains("đầm") || input.Contains("khoác") || input.Contains("size") ||
                                       input.Contains("chiều cao") || input.Contains("cân nặng") || input.Contains("đổi trả");

            if (isProductOrSizeQuery && matchedProducts.Count == 0 && string.IsNullOrWhiteSpace(knowledgeContext))
            {
                sw.Stop();
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = "Xin lỗi bạn, hiện tại trong hệ thống cửa hàng chưa có thông tin hoặc sản phẩm này.",
                    IsRag = true,
                    Route = "unknown",
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }

            // BƯỚC 4: XÂY DỰNG NGỮ CẢNH RAG & PROMPT
            var contextBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(knowledgeContext))
            {
                contextBuilder.AppendLine("=== DỮ LIỆU TÀI LIỆU CHÍNH SÁCH & BẢNG SIZE SHOP ===");
                contextBuilder.AppendLine(knowledgeContext);
                contextBuilder.AppendLine();
            }

            if (matchedProducts.Any())
            {
                contextBuilder.AppendLine("=== DANH SÁCH SẢN PHẨM PHÙ HỢP TẠI SHOP ===");
                foreach (var p in matchedProducts.Take(6))
                {
                    contextBuilder.AppendLine($"- [Mã: {p.Sku}] {p.Name} | Danh mục: {p.Category} | Size: {p.Size} | Màu: {p.Color} | Giá: {p.Price:N0} VNĐ | Tồn kho: {p.StockQuantity} | Mô tả: {p.Description}");
                }
                contextBuilder.AppendLine();
            }

            var systemPrompt = @"Bạn là trợ lý tư vấn bán hàng của Shop Quần Áo.
QUY TẮC BẮT BUỘC:
1. Bạn phải tư vấn dựa trên thông tin sản phẩm, bảng quy đổi size và chính sách đổi trả có trong phần BỐI CẢNH CỬA HÀNG được cung cấp.
2. TUYỆT ĐỐI KHÔNG tự ý bịa ra sản phẩm, giá bán hoặc chính sách không có trong tài liệu.
3. NẾU THÔNG TIN KHÔNG CÓ TRONG TÀI LIỆU: Chỉ cần trả lời lịch sự: 'Xin lỗi bạn, hiện tại hệ thống chưa có thông tin về câu hỏi này.'
4. Nếu khách hỏi size (kèm chiều cao, cân nặng), hãy đối chiếu chính xác với Bảng Tra Cứu Size và đưa ra lời khuyên chọn size.
5. Trình bày thân thiện, súc tích bằng tiếng Việt, dùng gạch đầu dòng (-) rõ ràng.";

            var userPromptWithContext = $"{contextBuilder}\nCâu hỏi của khách hàng: {input}";

            // BƯỚC 5: GỌI NHÀ CUNG CẤP AI THẬT (GEMINI / CLAUDE / OPENAI - BẮT BUỘC CALL API)
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                sw.Stop();
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = $"⚠️ Bạn chưa cấu hình API Key cho {_config.Provider}.\n\nVui lòng vào Tab 'Cấu Hình API Key & CSDL' để nhập API Key hợp lệ của {_config.Provider} để AI có thể sinh câu trả lời RAG.",
                    IsRag = true,
                    Route = decision.Route,
                    LatencyMs = (int)sw.ElapsedMilliseconds,
                    ReferencedProducts = matchedProducts
                };
            }

            var provider = GetProvider(_config.Provider);
            try
            {
                var (answer, latency) = await provider.GenerateResponseAsync(systemPrompt, userPromptWithContext, _config, ct);
                sw.Stop();

                return new ChatMessage
                {
                    Role = "assistant",
                    Content = answer,
                    IsRag = true,
                    Route = decision.Route,
                    LatencyMs = latency > 0 ? latency : (int)sw.ElapsedMilliseconds,
                    ReferencedProducts = matchedProducts
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new ChatMessage
                {
                    Role = "assistant",
                    Content = $"❌ Lỗi khi gửi yêu cầu tới AI {_config.Provider}:\n{ex.Message}\n\nVui lòng kiểm tra lại API Key trong phần Cài Đặt hoặc kiểm tra kết nối mạng.",
                    IsRag = true,
                    Route = decision.Route,
                    LatencyMs = (int)sw.ElapsedMilliseconds,
                    ReferencedProducts = matchedProducts
                };
            }
        }

        private RouteDecision EvaluateGuardrailsAndIntent(string query)
        {
            var qLower = query.ToLowerInvariant();

            // 1. Chặn Security Threat / Prompt Injection / SQL Injection
            if (qLower.Contains("ignore previous instructions") ||
                qLower.Contains("bỏ qua chỉ dẫn") ||
                qLower.Contains("system prompt") ||
                qLower.Contains("select * from") ||
                qLower.Contains("drop table") ||
                qLower.Contains("union select") ||
                qLower.Contains("delete from") ||
                qLower.Contains("jailbreak"))
            {
                return new RouteDecision
                {
                    Route = "security_threat",
                    Confidence = 0.99,
                    IsBlocked = true,
                    SuggestedAction = "block_and_alert",
                    SuggestedReply = "Yêu cầu bị từ chối do vi phạm chính sách bảo mật hệ thống."
                };
            }

            // 2. Ý định tư vấn size
            if (qLower.Contains("size") || qLower.Contains("chiều cao") || qLower.Contains("cân nặng") || qLower.Contains("nặng") || qLower.Contains("cao"))
            {
                return new RouteDecision { Route = "size_consultation", Confidence = 0.90 };
            }

            // 3. Ý định chính sách đổi trả / bảo quản
            if (qLower.Contains("đổi trả") || qLower.Contains("bảo hành") || qLower.Contains("giặt") || qLower.Contains("tích điểm") || qLower.Contains("voucher"))
            {
                return new RouteDecision { Route = "store_policy", Confidence = 0.90 };
            }

            return new RouteDecision { Route = "clothing_recommendation", Confidence = 0.85 };
        }

        private async Task<List<ClothingProduct>> RetrieveMatchingProductsAsync(string query, CancellationToken ct)
        {
            var allProducts = await _productDao.GetAllAsync(ct);
            var queryLower = query.ToLowerInvariant();

            var matches = allProducts.Where(p =>
                queryLower.Contains(p.Sku.ToLowerInvariant()) ||
                queryLower.Contains(p.Category.ToLowerInvariant()) ||
                queryLower.Contains(p.Name.ToLowerInvariant()) ||
                p.Name.ToLowerInvariant().Contains(queryLower)
            ).ToList();

            if (matches.Any()) return matches;

            var stopWords = new HashSet<string>
            {
                "bạn", "có", "không", "gì", "là", "ở", "trong", "được", "cho", "xin", "của",
                "tại", "shop", "cửa", "hàng", "tư", "vấn", "giúp", "mình", "tôi", "em", "tìm",
                "kiếm", "mua", "bán", "mẫu", "nào", "mấy", "bao", "nhiêu", "tiền", "giá"
            };

            var keywords = queryLower.Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                                     .ToList();

            if (keywords.Any())
            {
                matches = allProducts.Where(p =>
                    keywords.Any(kw =>
                        p.Name.ToLowerInvariant().Contains(kw) ||
                        p.Category.ToLowerInvariant().Contains(kw) ||
                        p.Color.ToLowerInvariant().Contains(kw) ||
                        (p.Description != null && p.Description.ToLowerInvariant().Contains(kw))
                    )
                ).ToList();
            }

            return matches;
        }

        private static IAiProvider GetProvider(string providerName) => providerName?.ToLowerInvariant() switch
        {
            "claude" => new ClaudeProvider(),
            "openai" => new OpenAiProvider(),
            _ => new GeminiProvider() // Mặc định là Google Gemini thật
        };
    }
}
