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
        private static readonly List<KnowledgeDocument> _localDocs = new()
        {
            new KnowledgeDocument
            {
                Id = 1,
                Category = "size_guide",
                Title = "Bảng Tra Cứu Size Chuẩn Người Việt Nam (Chiều Cao & Cân Nặng)",
                Content = @"HƯỚNG DẪN CHỌN SIZE QUẦN ÁO CHUẨN FORM SHOP:
- Size S: Chiều cao 1m50 - 1m60 | Cân nặng 42kg - 52kg (Vòng ngực 82-86cm, Vòng eo 64-68cm).
- Size M: Chiều cao 1m58 - 1m68 | Cân nặng 53kg - 62kg (Vòng ngực 86-92cm, Vòng eo 68-74cm). Thích hợp người 1m65 nặng 58kg.
- Size L: Chiều cao 1m66 - 1m75 | Cân nặng 63kg - 72kg (Vòng ngực 92-98cm, Vòng eo 74-82cm). Thích hợp người 1m70 nặng 65kg.
- Size XL: Chiều cao 1m72 - 1m82 | Cân nặng 73kg - 82kg (Vòng ngực 98-106cm, Vòng eo 82-90cm). Thích hợp người 1m75 nặng 78kg.
- Size XXL: Chiều cao 1m78 - 1m88 | Cân nặng 83kg - 95kg.
* LƯU Ý KHI CHỌN SIZE:
1. Nếu bạn thích mặc ôm gọn gàng hãy chọn đúng size. Nếu thích mặc form rộng thoải mái (Oversize) hãy tăng lên 1 size.
2. Với người có bụng hoặc cơ bắp to, nên ưu tiên chọn size theo cân nặng trước."
            },
            new KnowledgeDocument
            {
                Id = 2,
                Category = "return_policy",
                Title = "Chính Sách Đổi Trả Hàng Trong Vòng 7 Ngày",
                Content = @"QUY ĐỊNH ĐỔI TRẢ HÀNG TẠI CỬA HÀNG THỜI TRANG:
1. Thời hạn đổi trả: Khách hàng được đổi mẫu, đổi size trong vòng 07 ngày kể từ ngày nhận hàng.
2. Điều kiện sản phẩm: Sản phẩm còn nguyên tem mác, hoá đơn mua hàng, chưa qua sử dụng, chưa giặt tẩy và không có mùi lạ (nước hoa, mồ hôi).
3. Miễn phí đổi hàng: Đổi size hoặc đổi sản phẩm tương đương hoàn toàn miễn phí nếu do lỗi sản xuất (rách chỉ, lỗi cúc, phai màu) hoặc nhân viên gửi nhầm size.
4. Đổi theo ý thích cá nhân: Khách hàng chịu phí ship 2 chiều (khoảng 30.000 VNĐ) nếu muốn đổi màu hoặc đổi mẫu sang sản phẩm khác.
5. Không áp dụng đổi trả: Các sản phẩm trong danh mục xả kho giảm giá trên 50%, đồ lót, tất."
            },
            new KnowledgeDocument
            {
                Id = 3,
                Category = "care_guide",
                Title = "Cẩm Nang Giặt Là & Bảo Quản Trang Phục Bền Đẹp",
                Content = @"CÁCH BẢO QUẢN QUẦN ÁO KHÔNG BỊ XÙ LÔNG, PHAI MÀU:
1. Áo sơ mi & Quần Tây: Nên giặt ở chế độ giặt nhẹ (Gentle Wash), không vắt kiệt để tránh gãy nếp cổ áo. Phơi bằng móc bản to để giữ form vai áo.
2. Quần Jean / Denim: Trong lần giặt đầu tiên, nên ngâm nước muối pha loãng 2 tiếng để giữ màu vải luôn mới. Tuyệt đối không giặt chung đồ trắng với quần Jean màu đậm.
3. Áo Thun Cotton Compact & Đầm Voan: Nên lộn trái mặt vải trước khi giặt, tránh phơi trực tiếp dưới ánh nắng gay gắt ban trưa để không bị giòn sợi cotton.
4. Áo Khoác Gió Bomber: Tránh dùng chất tẩy javen mạnh, nên lau khăn ẩm hoặc giặt tay nhẹ nhàng."
            },
            new KnowledgeDocument
            {
                Id = 4,
                Category = "vip_promotion",
                Title = "Chính Sách Tích Điểm Thành Viên & Khuyến Mãi VIP",
                Content = @"CHƯƠNG TRÌNH KHÁCH HÀNG THÂN THIẾT SHOP:
- Khách hàng Mới: Tặng ngay voucher giảm giá 10% cho đơn hàng đầu tiên khi đăng ký tài khoản.
- Hạng Bạc (Silver - Tích luỹ trên 2.000.000đ): Giảm trực tiếp 5% cho tất cả đơn hàng, tặng voucher 100.000đ vào ngày sinh nhật.
- Hạng Vàng (Gold - Tích luỹ trên 5.000.000đ): Giảm 10% trọn đời mọi đơn hàng, miễn phí vận chuyển toàn quốc.
- Hạng Kim Cương (Diamond - Tích luỹ trên 10.000.000đ): Giảm 15% trọn đời, ưu tiên nhận thông báo bộ sưu tập mới trước 7 ngày, đổi trả linh hoạt trong 30 ngày."
            }
        };

        private readonly string _connectionString;

        public KnowledgeDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<string?> SearchKnowledgeAsync(string query, int limit = 3, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            // 1. Thử gọi hàm RPC search_knowledge_documents_text trên Supabase PostgreSQL
            if (!string.IsNullOrWhiteSpace(_connectionString))
            {
                try
                {
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

                    if (sb.Length > 0)
                    {
                        return sb.ToString().Trim();
                    }
                }
                catch
                {
                    // Fallback xuống bộ nhớ cục bộ
                }
            }

            // 2. Fallback tìm kiếm cục bộ nếu Supabase offline hoặc chưa cài RPC
            var queryLower = query.ToLowerInvariant();
            var matches = _localDocs.Where(d =>
                d.Title.ToLowerInvariant().Contains(queryLower) ||
                d.Content.ToLowerInvariant().Contains(queryLower) ||
                d.Category.ToLowerInvariant().Contains(queryLower)
            ).Take(limit).ToList();

            if (!matches.Any())
            {
                var stopWords = new HashSet<string> { "bạn", "cho", "tôi", "mình", "của", "các", "những", "được", "không", "nào", "với", "trong", "và", "hay", "là", "có", "thế", "nào", "gì", "ạ", "xin" };
                var words = queryLower.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                                      .ToList();

                if (words.Any())
                {
                    matches = _localDocs.Where(d =>
                        words.Any(w => d.Title.ToLowerInvariant().Contains(w) || d.Content.ToLowerInvariant().Contains(w))
                    ).Take(limit).ToList();
                }
            }

            if (matches.Any())
            {
                var sb = new StringBuilder();
                foreach (var doc in matches)
                {
                    sb.AppendLine($"[TÀI LIỆU CỬA HÀNG]: {doc.Title}");
                    sb.AppendLine(doc.Content);
                    sb.AppendLine();
                }
                return sb.ToString().Trim();
            }

            return null;
        }
    }
}
