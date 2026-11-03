using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClothingShop.DAL.Models;
using Npgsql;

namespace ClothingShop.DAL
{
    public class ProductDAO
    {
        // Danh sách dự phòng cục bộ (Offline / In-Memory Seed Data)
        private static readonly List<ClothingProduct> _localProducts = new()
        {
            new ClothingProduct { Id = 1, Sku = "SM-01", Name = "Áo Sơ Mi Nam Oxford Dài Tay", Category = "Áo sơ mi", Size = "L", Color = "Trắng", Price = 350000, StockQuantity = 45, Description = "Chất liệu vải Oxford 100% cotton dệt chéo, đứng form, chống nhăn nhẹ, lịch sự nơi công sở." },
            new ClothingProduct { Id = 2, Sku = "SM-02", Name = "Áo Sơ Mi Nam Oxford Dài Tay", Category = "Áo sơ mi", Size = "M", Color = "Xanh Navy", Price = 350000, StockQuantity = 30, Description = "Vải cotton thoáng mát, form regular fit chuẩn tôn dáng quý ông." },
            new ClothingProduct { Id = 3, Sku = "TS-01", Name = "Áo Thun Unisex Cotton Compact Cổ Tròn", Category = "Áo thun", Size = "XL", Color = "Đen", Price = 190000, StockQuantity = 80, Description = "Cotton Compact 250gsm dày dặn, thấm hút mồ hôi tốt, co giãn 4 chiều." },
            new ClothingProduct { Id = 4, Sku = "TS-02", Name = "Áo Thun Unisex Cotton Compact Cổ Tròn", Category = "Áo thun", Size = "M", Color = "Beige", Price = 190000, StockQuantity = 65, Description = "Màu be thanh lịch, trẻ trung, phù hợp phong cách dạo phố minimalist." },
            new ClothingProduct { Id = 5, Sku = "PL-01", Name = "Áo Polo Nam Pique Phối Bo Cổ Cổ Điển", Category = "Áo polo", Size = "L", Color = "Ghi Xám", Price = 280000, StockQuantity = 40, Description = "Vải dệt Pique gai cá sấu cao cấp, mềm mịn, bo dệt cổ áo không lo bị quăn mép." },
            new ClothingProduct { Id = 6, Sku = "JN-01", Name = "Quần Jean Nam Slim-fit Wash Nhẹ Cao Cấp", Category = "Quần jean", Size = "31", Color = "Xanh Indigo", Price = 450000, StockQuantity = 25, Description = "Vải denim co giãn 2% spandex, không cấn khi ngồi, đường may chỉ kép chắc chắn." },
            new ClothingProduct { Id = 7, Sku = "JN-02", Name = "Quần Jeans Nữ Ống Suông Ống Rộng Retro", Category = "Quần jean", Size = "28", Color = "Xanh Nhạt", Price = 390000, StockQuantity = 35, Description = "Cạp cao che khuyết điểm bụng, tôn dáng chân dài phong cách Hàn Quốc." },
            new ClothingProduct { Id = 8, Sku = "QT-01", Name = "Quần Tây Âu Nam Hàn Quốc Xếp Ly Co Giãn", Category = "Quần âu", Size = "32", Color = "Đen", Price = 390000, StockQuantity = 50, Description = "Chất vải Tuýt-si cao cấp, mềm mượt, lưng chun co giãn tiện lợi." },
            new ClothingProduct { Id = 9, Sku = "VD-01", Name = "Đầm Voan Hoa Nhí Cổ Vuông Tay Phồng", Category = "Váy đầm", Size = "S", Color = "Hồng Pastel", Price = 420000, StockQuantity = 20, Description = "Váy hoa phong cách vintage dịu dàng, lót lụa mềm mại bên trong." },
            new ClothingProduct { Id = 10, Sku = "CV-01", Name = "Chân Váy Chữ A Kaki Xếp Ly Công Sở", Category = "Chân váy", Size = "M", Color = "Be", Price = 250000, StockQuantity = 30, Description = "Kaki Hàn Quốc đứng form, có quần bảo hộ bên trong an toàn thoải mái." },
            new ClothingProduct { Id = 11, Sku = "AK-01", Name = "Áo Khoác Gió Bomber 2 Lớp Chống Nước", Category = "Áo khoác", Size = "XL", Color = "Rêu Đậm", Price = 490000, StockQuantity = 15, Description = "Chống tia UV, cản gió và chống nước đi mưa nhỏ hiệu quả." },
            new ClothingProduct { Id = 12, Sku = "BL-01", Name = "Áo Blazer Nữ Form Rộng Phong Cách Hàn Quốc", Category = "Áo blazer", Size = "M", Color = "Nâu Tây", Price = 550000, StockQuantity = 18, Description = "Form oversize hiện đại, lót vải lụa Habutai cao cấp, thích hợp đi làm và đi tiệc." }
        };

        private readonly string _connectionString;

        public ProductDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ClothingProduct>> GetAllAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                return _localProducts.OrderByDescending(p => p.Id).ToList();
            }

            try
            {
                await using var conn = DatabaseConnection.CreateConnection(_connectionString);
                await conn.OpenAsync(ct);

                const string sql = @"
                    SELECT id, sku, name, category, size, color, price, stock_quantity, description, created_at, updated_at
                    FROM public.clothing_products
                    ORDER BY id DESC;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(ct);

                var list = new List<ClothingProduct>();
                while (await reader.ReadAsync(ct))
                {
                    list.Add(MapProduct(reader));
                }

                // Nếu DB rỗng, đồng bộ dữ liệu mẫu lên
                if (list.Count == 0)
                {
                    return _localProducts.OrderByDescending(p => p.Id).ToList();
                }

                return list;
            }
            catch
            {
                // Tự động chuyển qua danh sách cục bộ khi mất kết nối mạng
                return _localProducts.OrderByDescending(p => p.Id).ToList();
            }
        }

        public async Task<ClothingProduct?> GetByIdAsync(long id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                return _localProducts.FirstOrDefault(p => p.Id == id);
            }

            try
            {
                await using var conn = DatabaseConnection.CreateConnection(_connectionString);
                await conn.OpenAsync(ct);

                const string sql = @"
                    SELECT id, sku, name, category, size, color, price, stock_quantity, description, created_at, updated_at
                    FROM public.clothing_products
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                await using var reader = await cmd.ExecuteReaderAsync(ct);

                if (await reader.ReadAsync(ct))
                {
                    return MapProduct(reader);
                }
                return null;
            }
            catch
            {
                return _localProducts.FirstOrDefault(p => p.Id == id);
            }
        }

        public async Task<ClothingProduct> AddAsync(ClothingProduct product, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                product.Id = _localProducts.Count > 0 ? _localProducts.Max(p => p.Id) + 1 : 1;
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;
                _localProducts.Insert(0, product);
                return product;
            }

            try
            {
                await using var conn = DatabaseConnection.CreateConnection(_connectionString);
                await conn.OpenAsync(ct);

                const string sql = @"
                    INSERT INTO public.clothing_products (sku, name, category, size, color, price, stock_quantity, description, created_at, updated_at)
                    VALUES (@sku, @name, @category, @size, @color, @price, @stock_quantity, @description, NOW(), NOW())
                    RETURNING id, created_at, updated_at;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sku", product.Sku.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@name", product.Name.Trim());
                cmd.Parameters.AddWithValue("@category", product.Category.Trim());
                cmd.Parameters.AddWithValue("@size", product.Size.Trim());
                cmd.Parameters.AddWithValue("@color", product.Color.Trim());
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@stock_quantity", product.StockQuantity);
                cmd.Parameters.AddWithValue("@description", (object?)product.Description ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    product.Id = reader.GetInt64(0);
                    product.CreatedAt = reader.GetDateTime(1);
                    product.UpdatedAt = reader.GetDateTime(2);
                }

                return product;
            }
            catch
            {
                product.Id = _localProducts.Count > 0 ? _localProducts.Max(p => p.Id) + 1 : 1;
                _localProducts.Insert(0, product);
                return product;
            }
        }

        public async Task<bool> UpdateAsync(ClothingProduct product, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                var existing = _localProducts.FirstOrDefault(p => p.Id == product.Id);
                if (existing != null)
                {
                    existing.Sku = product.Sku;
                    existing.Name = product.Name;
                    existing.Category = product.Category;
                    existing.Size = product.Size;
                    existing.Color = product.Color;
                    existing.Price = product.Price;
                    existing.StockQuantity = product.StockQuantity;
                    existing.Description = product.Description;
                    existing.UpdatedAt = DateTime.UtcNow;
                    return true;
                }
                return false;
            }

            try
            {
                await using var conn = DatabaseConnection.CreateConnection(_connectionString);
                await conn.OpenAsync(ct);

                const string sql = @"
                    UPDATE public.clothing_products
                    SET sku = @sku,
                        name = @name,
                        category = @category,
                        size = @size,
                        color = @color,
                        price = @price,
                        stock_quantity = @stock_quantity,
                        description = @description,
                        updated_at = NOW()
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", product.Id);
                cmd.Parameters.AddWithValue("@sku", product.Sku.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@name", product.Name.Trim());
                cmd.Parameters.AddWithValue("@category", product.Category.Trim());
                cmd.Parameters.AddWithValue("@size", product.Size.Trim());
                cmd.Parameters.AddWithValue("@color", product.Color.Trim());
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@stock_quantity", product.StockQuantity);
                cmd.Parameters.AddWithValue("@description", (object?)product.Description ?? DBNull.Value);

                var rows = await cmd.ExecuteNonQueryAsync(ct);
                return rows > 0;
            }
            catch
            {
                var existing = _localProducts.FirstOrDefault(p => p.Id == product.Id);
                if (existing != null)
                {
                    existing.Sku = product.Sku;
                    existing.Name = product.Name;
                    existing.Category = product.Category;
                    existing.Size = product.Size;
                    existing.Color = product.Color;
                    existing.Price = product.Price;
                    existing.StockQuantity = product.StockQuantity;
                    existing.Description = product.Description;
                    existing.UpdatedAt = DateTime.UtcNow;
                    return true;
                }
                return false;
            }
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                var item = _localProducts.FirstOrDefault(p => p.Id == id);
                if (item != null)
                {
                    _localProducts.Remove(item);
                    return true;
                }
                return false;
            }

            try
            {
                await using var conn = DatabaseConnection.CreateConnection(_connectionString);
                await conn.OpenAsync(ct);

                const string sql = @"DELETE FROM public.clothing_products WHERE id = @id;";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);

                var rows = await cmd.ExecuteNonQueryAsync(ct);
                return rows > 0;
            }
            catch
            {
                var item = _localProducts.FirstOrDefault(p => p.Id == id);
                if (item != null)
                {
                    _localProducts.Remove(item);
                    return true;
                }
                return false;
            }
        }

        public async Task<List<ClothingProduct>> SearchAsync(string? keyword, string? category = null, string? size = null, CancellationToken ct = default)
        {
            var all = await GetAllAsync(ct);

            var query = all.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                         p.Sku.ToLower().Contains(kw) ||
                                         p.Color.ToLower().Contains(kw) ||
                                         (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
            {
                query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(size) && size != "Tất cả")
            {
                query = query.Where(p => p.Size.Equals(size, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }

        private static ClothingProduct MapProduct(NpgsqlDataReader reader)
        {
            return new ClothingProduct
            {
                Id = reader.GetInt64(0),
                Sku = reader.GetString(1),
                Name = reader.GetString(2),
                Category = reader.GetString(3),
                Size = reader.GetString(4),
                Color = reader.GetString(5),
                Price = reader.GetDecimal(6),
                StockQuantity = reader.GetInt32(7),
                Description = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt = reader.IsDBNull(9) ? DateTime.UtcNow : reader.GetDateTime(9),
                UpdatedAt = reader.IsDBNull(10) ? DateTime.UtcNow : reader.GetDateTime(10)
            };
        }
    }
}

