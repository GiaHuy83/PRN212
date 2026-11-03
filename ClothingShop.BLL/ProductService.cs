using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClothingShop.DAL;
using ClothingShop.DAL.Models;

namespace ClothingShop.BLL
{
    public class ProductService
    {
        private readonly ProductDAO _productDao;

        public ProductService(string connectionString)
        {
            _productDao = new ProductDAO(connectionString);
        }

        public async Task<List<ClothingProduct>> GetAllProductsAsync(CancellationToken ct = default)
        {
            return await _productDao.GetAllAsync(ct);
        }

        public async Task<ClothingProduct?> GetProductByIdAsync(long id, CancellationToken ct = default)
        {
            return await _productDao.GetByIdAsync(id, ct);
        }

        public async Task<(bool success, string message, ClothingProduct? product)> AddProductAsync(ClothingProduct product, CancellationToken ct = default)
        {
            var (isValid, errorMsg) = ValidateProduct(product);
            if (!isValid)
            {
                return (false, errorMsg, null);
            }

            // Kiểm tra trùng mã SKU
            var existing = await _productDao.GetAllAsync(ct);
            if (existing.Any(p => p.Sku.Equals(product.Sku.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"Mã SKU '{product.Sku.Trim().ToUpper()}' đã tồn tại trong hệ thống.", null);
            }

            var created = await _productDao.AddAsync(product, ct);
            return (true, "Thêm sản phẩm thành công!", created);
        }

        public async Task<(bool success, string message)> UpdateProductAsync(ClothingProduct product, CancellationToken ct = default)
        {
            var (isValid, errorMsg) = ValidateProduct(product);
            if (!isValid)
            {
                return (false, errorMsg);
            }

            // Kiểm tra trùng mã SKU với sản phẩm khác
            var existing = await _productDao.GetAllAsync(ct);
            if (existing.Any(p => p.Id != product.Id && p.Sku.Equals(product.Sku.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"Mã SKU '{product.Sku.Trim().ToUpper()}' đã thuộc về một sản phẩm khác.");
            }

            var ok = await _productDao.UpdateAsync(product, ct);
            return ok ? (true, "Cập nhật sản phẩm thành công!") : (false, "Không tìm thấy sản phẩm để cập nhật.");
        }

        public async Task<(bool success, string message)> DeleteProductAsync(long id, CancellationToken ct = default)
        {
            var ok = await _productDao.DeleteAsync(id, ct);
            return ok ? (true, "Xóa sản phẩm thành công!") : (false, "Không tìm thấy sản phẩm để xóa.");
        }

        public async Task<List<ClothingProduct>> SearchProductsAsync(string? keyword, string? category = null, string? size = null, CancellationToken ct = default)
        {
            return await _productDao.SearchAsync(keyword, category, size, ct);
        }

        public (int totalItems, int totalQuantity, decimal totalValue, int outOfStock) CalculateStats(List<ClothingProduct> products)
        {
            if (products == null || products.Count == 0)
            {
                return (0, 0, 0, 0);
            }

            int totalItems = products.Count;
            int totalQuantity = products.Sum(p => p.StockQuantity);
            decimal totalValue = products.Sum(p => p.Price * p.StockQuantity);
            int outOfStock = products.Count(p => p.StockQuantity <= 0);

            return (totalItems, totalQuantity, totalValue, outOfStock);
        }

        private static (bool isValid, string errorMessage) ValidateProduct(ClothingProduct p)
        {
            if (string.IsNullOrWhiteSpace(p.Sku))
                return (false, "Mã SKU không được để trống.");

            if (string.IsNullOrWhiteSpace(p.Name))
                return (false, "Tên sản phẩm không được để trống.");

            if (string.IsNullOrWhiteSpace(p.Category))
                return (false, "Vui lòng chọn hoặc nhập Danh mục sản phẩm.");

            if (string.IsNullOrWhiteSpace(p.Size))
                return (false, "Vui lòng chọn hoặc nhập Size sản phẩm.");

            if (string.IsNullOrWhiteSpace(p.Color))
                return (false, "Vui lòng nhập Màu sắc sản phẩm.");

            if (p.Price <= 0)
                return (false, "Giá sản phẩm phải lớn hơn 0 VNĐ.");

            if (p.StockQuantity < 0)
                return (false, "Số lượng tồn kho không được âm.");

            return (true, string.Empty);
        }
    }
}

