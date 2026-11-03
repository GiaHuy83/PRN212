using System;

namespace ClothingShop.DAL.Models
{
    public class ClothingProduct
    {
        public long Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string FormattedPrice => $"{Price:N0} VNĐ";
        public string StockStatus => StockQuantity > 0 ? $"Còn hàng ({StockQuantity})" : "Hết hàng";
    }
}

