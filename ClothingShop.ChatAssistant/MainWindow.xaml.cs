using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClothingShop.BLL;
using ClothingShop.DAL;
using ClothingShop.DAL.Models;

namespace ClothingShop.ChatAssistant
{
    public partial class MainWindow : Window
    {
        private AppConfig _config;
        private AiChatService _aiChatService;
        private readonly ObservableCollection<ChatMessage> _chatMessages = new();

        public MainWindow()
        {
            InitializeComponent();

            _config = ConfigDAO.LoadConfig();
            _aiChatService = new AiChatService(_config.SupabaseConnectionString, _config);

            icChatMessages.ItemsSource = _chatMessages;
            InitWelcomeChatMessage();
            LoadConfigToSettingsTab();
        }

        private void InitWelcomeChatMessage()
        {
            _chatMessages.Clear();
            _chatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = "Xin chào bạn! Tôi là Trợ Lý Thời Trang AI của Shop Quần Áo.\n\n" +
                          "Hệ thống kết nối trực tiếp với API trí tuệ nhân tạo (Gemini / Claude / OpenAI):\n" +
                          "• 📏 Tư vấn chọn size áo, quần, váy chuẩn theo chiều cao và cân nặng.\n" +
                          "• 👗 Tìm kiếm sản phẩm trong kho (Sơ mi Oxford, Áo thun, Quần Jean, Đầm...).\n" +
                          "• 🔄 Giải đáp chính sách đổi trả hàng trong 7 ngày, cách giặt là bảo quản.\n\n" +
                          "Bạn cần tìm sản phẩm hoặc tư vấn gì hôm nay ạ?",
                IsRag = true,
                Route = "chitchat",
                LatencyMs = 0
            });
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            // Tự động đồng bộ cấu hình mới nhất từ Tab Cài Đặt (ngay cả khi quên bấm Lưu)
            ApplyCurrentSettingsFromUi(saveToFile: true);

            var text = txtChatInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            txtChatInput.Text = string.Empty;
            btnSend.IsEnabled = false;

            var userMsg = new ChatMessage
            {
                Role = "user",
                Content = text,
                CreatedAt = DateTime.UtcNow
            };
            _chatMessages.Add(userMsg);
            ScrollChatToBottom();

            bool isRag = rbRagMode.IsChecked == true;

            try
            {
                var reply = await _aiChatService.SendMessageAsync(text, isRag);
                _chatMessages.Add(reply);
                ScrollChatToBottom();
            }
            catch (Exception ex)
            {
                _chatMessages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = $"❌ Đã xảy ra lỗi: {ex.Message}",
                    IsRag = isRag,
                    Route = "general_chat",
                    LatencyMs = 0
                });
                ScrollChatToBottom();
            }
            finally
            {
                btnSend.IsEnabled = true;
                txtChatInput.Focus();
            }
        }

        private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSend_Click(sender, e);
            }
        }

        private void PromptPill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string prompt)
            {
                var cleanPrompt = System.Text.RegularExpressions.Regex.Replace(prompt, @"^[^\w\d\s]+", "").Trim();
                txtChatInput.Text = cleanPrompt;
                BtnSend_Click(sender, e);
            }
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            InitWelcomeChatMessage();
        }

        private void ScrollChatToBottom()
        {
            chatScrollViewer.Dispatcher.InvokeAsync(() =>
            {
                chatScrollViewer.ScrollToEnd();
            });
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tự động lưu và cập nhật cấu hình khi chuyển giữa các tab
            if (IsLoaded)
            {
                ApplyCurrentSettingsFromUi(saveToFile: true);
            }
        }

        private void LoadConfigToSettingsTab()
        {
            switch (_config.Provider?.ToLowerInvariant())
            {
                case "claude":
                    cboProvider.SelectedIndex = 2;
                    break;
                case "openai":
                    cboProvider.SelectedIndex = 1;
                    break;
                default:
                    cboProvider.SelectedIndex = 0; // Gemini
                    break;
            }

            txtApiKey.Text = _config.ApiKey ?? string.Empty;
            txtModelName.Text = !string.IsNullOrWhiteSpace(_config.ModelName) ? _config.ModelName : "gemini-1.5-flash";
            txtBaseUrl.Text = _config.BaseUrl ?? string.Empty;
            txtSupabaseConn.Text = _config.SupabaseConnectionString ?? string.Empty;

            UpdateStatusBadges();
        }

        private void CboProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selected = (cboProvider.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            if (selected.Contains("Claude"))
            {
                txtModelName.Text = "claude-3-5-sonnet-20241022";
                txtBaseUrl.Text = "https://api.anthropic.com/v1/messages";
            }
            else if (selected.Contains("OpenAI"))
            {
                txtModelName.Text = "gpt-4o-mini";
                txtBaseUrl.Text = "https://api.openai.com/v1/chat/completions";
            }
            else
            {
                txtModelName.Text = "gemini-1.5-flash";
                txtBaseUrl.Text = string.Empty;
            }

            ApplyCurrentSettingsFromUi(saveToFile: true);
        }

        private void ApplyCurrentSettingsFromUi(bool saveToFile = true)
        {
            if (!IsLoaded) return;

            _config.Provider = GetSelectedProviderName();
            _config.ApiKey = txtApiKey.Text.Trim();
            _config.ModelName = txtModelName.Text.Trim();
            _config.BaseUrl = txtBaseUrl.Text.Trim();
            _config.SupabaseConnectionString = txtSupabaseConn.Text.Trim();

            _aiChatService.UpdateConfig(_config);
            UpdateStatusBadges();

            if (saveToFile)
            {
                ConfigDAO.SaveConfig(_config);
            }
        }

        private void UpdateStatusBadges()
        {
            lblAiProviderStatus.Text = $"AI: {_config.Provider}";
            lblDbStatus.Text = !string.IsNullOrWhiteSpace(_config.SupabaseConnectionString)
                               && !_config.SupabaseConnectionString.Contains("[YOUR-PASSWORD]")
                               && !_config.SupabaseConnectionString.Contains("[YOUR-PROJECT-REF]")
                ? "CSDL: Supabase Connected" 
                : "CSDL: Local Cache";
        }

        private async void BtnTestAi_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = txtApiKey.Text.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("Vui lòng nhập API Key trước khi kiểm tra.", "Thiếu API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtApiKey.Focus();
                return;
            }

            var tempConfig = new AppConfig
            {
                Provider = GetSelectedProviderName(),
                ApiKey = apiKey,
                ModelName = txtModelName.Text.Trim(),
                BaseUrl = txtBaseUrl.Text.Trim()
            };

            btnTestAi.IsEnabled = false;
            btnTestAi.Content = "Đang gửi yêu cầu tới AI...";

            try
            {
                var testService = new AiChatService(string.Empty, tempConfig);
                var response = await testService.SendMessageAsync("Kiểm tra kết nối: Bạn là trợ lý thời trang của shop, hãy chào tôi 1 câu ngắn.", false);

                MessageBox.Show(
                    $"Gọi API thành công đến '{tempConfig.Provider}'!\n" +
                    $"Độ trễ phản hồi: {response.LatencyMs}ms\n\n" +
                    $"Phản hồi từ AI thật:\n{response.Content}",
                    "Kiểm tra AI thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gọi API AI thất bại:\n{ex.Message}", "Lỗi API AI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTestAi.IsEnabled = true;
                btnTestAi.Content = "🧪 Kiểm Tra Kết Nối API AI Ngay";
            }
        }

        private async void BtnTestDb_Click(object sender, RoutedEventArgs e)
        {
            var connStr = txtSupabaseConn.Text.Trim();
            btnTestDb.IsEnabled = false;
            btnTestDb.Content = "Đang kiểm tra CSDL...";

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
                btnTestDb.Content = "⚡ Kiểm Tra Kết Nối Supabase";
            }
        }

        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            ApplyCurrentSettingsFromUi(saveToFile: true);
            MessageBox.Show($"Đã lưu cấu hình thành công!\n\n- Nhà cung cấp AI: {_config.Provider}\n- Model: {_config.ModelName}\n- API Key: đã được ghi nhớ.", "Đã lưu cấu hình", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetSelectedProviderName()
        {
            var selected = (cboProvider.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            if (selected.Contains("Claude")) return "Claude";
            if (selected.Contains("OpenAI")) return "OpenAI";
            return "Gemini";
        }
    }
}
