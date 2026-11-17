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
        private readonly string _connectionString;

        public ProductDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        private void EnsureConfigured()
        {
            if (!DatabaseConnection.IsConfigured(_connectionString))
            {
                throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối Supabase PostgreSQL. Vui lòng vào tab Cấu Hình CSDL Supabase để nhập Connection String thật.");
            }
        }

        public async Task<List<ClothingProduct>> GetAllAsync(CancellationToken ct = default)
        {
            EnsureConfigured();

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

            return list;
        }

        public async Task<ClothingProduct?> GetByIdAsync(long id, CancellationToken ct = default)
        {
            EnsureConfigured();

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

        public async Task<ClothingProduct> AddAsync(ClothingProduct product, CancellationToken ct = default)
        {
            EnsureConfigured();

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

        public async Task<bool> UpdateAsync(ClothingProduct product, CancellationToken ct = default)
        {
            EnsureConfigured();

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

        public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
        {
            EnsureConfigured();

            await using var conn = DatabaseConnection.CreateConnection(_connectionString);
            await conn.OpenAsync(ct);

            const string sql = @"DELETE FROM public.clothing_products WHERE id = @id;";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
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

