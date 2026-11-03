using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClothingShop.ChatAssistant
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (Invert) b = !b;
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                var isVisible = v == Visibility.Visible;
                return Invert ? !isVisible : isVisible;
            }
            return false;
        }
    }
}

