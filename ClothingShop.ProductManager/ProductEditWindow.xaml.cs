using System;
using System.Windows;
using System.Windows.Controls;
using ClothingShop.DAL.Models;

namespace ClothingShop.ProductManager
{
    public partial class ProductEditWindow : Window
    {
        public ClothingProduct Product { get; private set; }

        public ProductEditWindow(ClothingProduct? productToEdit = null)
        {
            InitializeComponent();

            if (productToEdit != null)
            {
                Product = new ClothingProduct
                {
                    Id = productToEdit.Id,
                    Sku = productToEdit.Sku,
                    Name = productToEdit.Name,
                    Category = productToEdit.Category,
                    Size = productToEdit.Size,
                    Color = productToEdit.Color,
                    Price = productToEdit.Price,
                    StockQuantity = productToEdit.StockQuantity,
                    Description = productToEdit.Description,
                    CreatedAt = productToEdit.CreatedAt
                };

                txtTitle.Text = "Cập Nhật Sản Phẩm";
                txtSku.Text = Product.Sku;
                txtName.Text = Product.Name;
                cboCategory.Text = Product.Category;
                cboSize.Text = Product.Size;
                txtColor.Text = Product.Color;
                txtPrice.Text = Product.Price.ToString("0");
                txtStock.Text = Product.StockQuantity.ToString();
                txtDescription.Text = Product.Description ?? string.Empty;
            }
            else
            {
                Product = new ClothingProduct();
                txtTitle.Text = "Thêm Sản Phẩm Mới";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var sku = txtSku.Text.Trim();
            var name = txtName.Text.Trim();
            var category = cboCategory.Text.Trim();
            var size = cboSize.Text.Trim();
            var color = txtColor.Text.Trim();

            if (string.IsNullOrWhiteSpace(sku))
            {
                MessageBox.Show("Vui lòng nhập Mã SKU cho sản phẩm.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSku.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập Tên sản phẩm.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập Danh mục.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                cboCategory.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(size))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập Size.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                cboSize.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(color))
            {
                MessageBox.Show("Vui lòng nhập Màu sắc sản phẩm.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtColor.Focus();
                return;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out var price) || price <= 0)
            {
                MessageBox.Show("Đơn giá phải là số hợp lệ và lớn hơn 0 VNĐ.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrice.Focus();
                return;
            }

            if (!int.TryParse(txtStock.Text.Trim(), out var stock) || stock < 0)
            {
                MessageBox.Show("Số lượng tồn kho phải là số nguyên không âm.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStock.Focus();
                return;
            }

            Product.Sku = sku.ToUpper();
            Product.Name = name;
            Product.Category = category;
            Product.Size = size;
            Product.Color = color;
            Product.Price = price;
            Product.StockQuantity = stock;
            Product.Description = txtDescription.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

