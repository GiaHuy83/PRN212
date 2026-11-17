using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClothingShop.BLL;
using ClothingShop.DAL;
using ClothingShop.DAL.Models;

namespace ClothingShop.ProductManager
{
    public partial class MainWindow : Window
    {
        private AppConfig _config;
        private ProductService _productService;

        public MainWindow()
        {
            InitializeComponent();

            _config = ConfigDAO.LoadConfig();
            _productService = new ProductService(_config.SupabaseConnectionString);

            txtSupabaseConn.Text = _config.SupabaseConnectionString ?? string.Empty;
            UpdateDbStatus();

            Loaded += async (s, e) =>
            {
                await LoadProductsAsync();
            };
        }

        private void UpdateDbStatus(bool? connectionSucceeded = null)
        {
            if (!DatabaseConnection.IsConfigured(_config.SupabaseConnectionString))
            {
                lblDbStatus.Text = "⚡ CSDL: Chưa cấu hình Supabase";
                return;
            }

            lblDbStatus.Text = connectionSucceeded == false
                ? "⚡ CSDL: Lỗi kết nối Supabase"
                : "⚡ CSDL: Supabase Connected";
        }

        private async Task LoadProductsAsync(string? keyword = null, string? category = null, string? size = null)
        {
            if (!DatabaseConnection.IsConfigured(_config.SupabaseConnectionString))
            {
                dgProducts.ItemsSource = null;
                txtMetricTotalItems.Text = "0";
                txtMetricTotalQty.Text = "0 cái";
                txtMetricTotalValue.Text = "0 VNĐ";
                txtMetricOutOfStock.Text = "0 mã";
                UpdateDbStatus();
                txtCrudStatus.Text = "Chưa cấu hình kết nối Supabase. Vui lòng vào tab Cấu Hình CSDL Supabase để kết nối.";
                return;
            }

            try
            {
                txtCrudStatus.Text = "Đang tải dữ liệu sản phẩm từ Supabase...";
                List<ClothingProduct> products;

                if (!string.IsNullOrWhiteSpace(keyword) || !string.IsNullOrWhiteSpace(category) || !string.IsNullOrWhiteSpace(size))
                {
                    products = await _productService.SearchProductsAsync(keyword, category, size);
                }
                else
                {
                    products = await _productService.GetAllProductsAsync();
                }

                dgProducts.ItemsSource = products;

                var (totalItems, totalQty, totalValue, outOfStock) = _productService.CalculateStats(products);
                txtMetricTotalItems.Text = totalItems.ToString("N0");
                txtMetricTotalQty.Text = $"{totalQty:N0} cái";
                txtMetricTotalValue.Text = $"{totalValue:N0} VNĐ";
                txtMetricOutOfStock.Text = $"{outOfStock:N0} mã";

                UpdateDbStatus(connectionSucceeded: true);
                txtCrudStatus.Text = $"Đã tải {products.Count} sản phẩm lúc {DateTime.Now:HH:mm:ss}.";
            }
            catch (Exception ex)
            {
                UpdateDbStatus(connectionSucceeded: false);
                txtCrudStatus.Text = $"Lỗi tải dữ liệu Supabase: {ex.Message}";
                MessageBox.Show($"Không thể kết nối/tải dữ liệu từ Supabase:\n{ex.Message}", "Lỗi CSDL Supabase", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var keyword = txtSearchKeyword.Text.Trim();
            var category = (cboCategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var size = (cboSizeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            await LoadProductsAsync(keyword, category, size);
        }

        private async void TxtSearchKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSearch_Click(sender, e);
            }
        }

        private async void CboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var keyword = txtSearchKeyword?.Text?.Trim();
            var category = (cboCategoryFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var size = (cboSizeFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            await LoadProductsAsync(keyword, category, size);
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearchKeyword.Text = string.Empty;
            if (cboCategoryFilter.Items.Count > 0) cboCategoryFilter.SelectedIndex = 0;
            if (cboSizeFilter.Items.Count > 0) cboSizeFilter.SelectedIndex = 0;
            await LoadProductsAsync();
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new ProductEditWindow
            {
                Owner = this
            };

            if (addWindow.ShowDialog() == true)
            {
                var newProduct = addWindow.Product;
                var (success, msg, _) = await _productService.AddProductAsync(newProduct);
                if (success)
                {
                    MessageBox.Show(msg, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi thêm sản phẩm", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is not ClothingProduct selected)
            {
                MessageBox.Show("Vui lòng chọn một sản phẩm trong bảng để chỉnh sửa.", "Chưa chọn sản phẩm", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editWindow = new ProductEditWindow(selected)
            {
                Owner = this
            };

            if (editWindow.ShowDialog() == true)
            {
                var updated = editWindow.Product;
                var (success, msg) = await _productService.UpdateProductAsync(updated);
                if (success)
                {
                    MessageBox.Show(msg, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi cập nhật", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is not ClothingProduct selected)
            {
                MessageBox.Show("Vui lòng chọn một sản phẩm trong bảng để xóa.", "Chưa chọn sản phẩm", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa sản phẩm '{selected.Name}' (Mã SKU: {selected.Sku})?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                var (success, msg) = await _productService.DeleteProductAsync(selected.Id);
                if (success)
                {
                    MessageBox.Show(msg, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void BtnTestDb_Click(object sender, RoutedEventArgs e)
        {
            var connStr = txtSupabaseConn.Text.Trim();
            btnTestDb.IsEnabled = false;
            btnTestDb.Content = "Đang kiểm tra...";

            try
            {
                var (success, msg) = await DatabaseConnection.TestConnectionAsync(connStr);
                if (success)
                {
                    MessageBox.Show(msg, "Kết nối CSDL thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi kết nối Supabase", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                btnTestDb.IsEnabled = true;
                btnTestDb.Content = "⚡ Kiểm Tra Kết Nối CSDL";
            }
        }

        private async void BtnSaveDb_Click(object sender, RoutedEventArgs e)
        {
            var connStr = txtSupabaseConn.Text.Trim();
            _config.SupabaseConnectionString = connStr;

            var saved = ConfigDAO.SaveConfig(_config);
            if (!saved)
            {
                MessageBox.Show("Lỗi khi lưu file cấu hình.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _productService = new ProductService(_config.SupabaseConnectionString);

            if (!DatabaseConnection.IsConfigured(connStr))
            {
                UpdateDbStatus();
                MessageBox.Show("Chuỗi kết nối chưa hợp lệ (đang rỗng hoặc còn placeholder [YOUR-PASSWORD] / [YOUR-PROJECT-REF]). Vui lòng dán Connection String thật từ Supabase.", "Thiếu thông tin kết nối", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadProductsAsync();
                return;
            }

            btnSaveDb.IsEnabled = false;
            btnSaveDb.Content = "Đang kết nối...";
            try
            {
                var (ok, msg) = await DatabaseConnection.TestConnectionAsync(connStr);
                if (ok)
                {
                    UpdateDbStatus(connectionSucceeded: true);
                    MessageBox.Show($"Đã lưu cấu hình và kết nối Supabase thành công!\n\n{msg}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    UpdateDbStatus(connectionSucceeded: false);
                    MessageBox.Show($"Đã lưu chuỗi kết nối vào appsettings.json, nhưng KẾT NỐI SUPABASE THẤT BẠI:\n{msg}", "Lỗi kết nối Supabase", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                btnSaveDb.IsEnabled = true;
                btnSaveDb.Content = "💾 Lưu Cấu Hình";
            }
        }
    }
}
