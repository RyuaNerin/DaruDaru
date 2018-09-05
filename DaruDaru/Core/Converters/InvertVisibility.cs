using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DaruDaru.Core.Converters
{
    internal class InvertVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility vis && vis == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility vis && vis == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
