using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ClothingShop.ChatAssistant
{
    public static class MarkdownHelper
    {
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.RegisterAttached("Markdown", typeof(string), typeof(MarkdownHelper),
                new PropertyMetadata(null, OnMarkdownChanged));

        public static void SetMarkdown(DependencyObject element, string value) => element.SetValue(MarkdownProperty, value);
        public static string GetMarkdown(DependencyObject element) => (string)element.GetValue(MarkdownProperty);

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock tb) return;

            tb.Inlines.Clear();
            var raw = e.NewValue as string;
            if (string.IsNullOrWhiteSpace(raw)) return;

            var lines = raw.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    // Thêm khoảng cách giữa các đoạn
                    tb.Inlines.Add(new LineBreak());
                    continue;
                }

                // Nhận diện cấp độ thụt đầu dòng (Nested bullet hoặc Top-level bullet)
                bool isNestedBullet = line.StartsWith("   * ") || line.StartsWith("  * ") || line.StartsWith("    * ") || 
                                     line.StartsWith("\t* ") || line.StartsWith("   - ") || line.StartsWith("  - ") || 
                                     line.StartsWith("    - ") || line.StartsWith("\t- ");
                bool isTopBullet = !isNestedBullet && (trimmed.StartsWith("* ") || trimmed.StartsWith("- ") || trimmed.StartsWith("• "));

                if (isNestedBullet)
                {
                    trimmed = Regex.Replace(trimmed, @"^[\*\-\•]\s*", "").Trim();
                    // Thụt lề và icon gạch đầu dòng con
                    tb.Inlines.Add(new Run("      ▫ ") 
                    { 
                        Foreground = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1)), // Indigo
                        FontWeight = FontWeights.Bold,
                        FontSize = 11
                    });
                }
                else if (isTopBullet)
                {
                    trimmed = Regex.Replace(trimmed, @"^[\*\-\•]\s*", "").Trim();
                    // Thụt lề và icon chấm đầu dòng chính
                    tb.Inlines.Add(new Run("  • ") 
                    { 
                        Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), // Blue
                        FontWeight = FontWeights.Bold,
                        FontSize = 14
                    });
                }

                // Xử lý in đậm (**nội dung**) và chữ thường
                ParseInlineFormatting(tb, trimmed);

                // Xuống dòng nếu không phải dòng cuối
                if (i < lines.Length - 1)
                {
                    tb.Inlines.Add(new LineBreak());
                }
            }
        }

        private static void ParseInlineFormatting(TextBlock tb, string text)
        {
            // Tách theo cặp **...**
            var parts = Regex.Split(text, @"(\*\*.*?\*\*)");
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("**") && part.EndsWith("**") && part.Length >= 4)
                {
                    var boldText = part.Substring(2, part.Length - 4);
                    tb.Inlines.Add(new Run(boldText)
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)) // Slate 900
                    });
                }
                else
                {
                    tb.Inlines.Add(new Run(part)
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)) // Slate 700
                    });
                }
            }
        }
    }
}

