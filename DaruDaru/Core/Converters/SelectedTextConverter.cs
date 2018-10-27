using System;
using System.Globalization;
using System.Windows.Data;

namespace DaruDaru.Core.Converters
{
    internal class SelectedTextConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            var count    = (int)value[0];
            var selected = (int)value[1];

            return selected > 1 ? string.Format("{0:N0} / {1:N0}", selected, count) : count.ToString("N0");
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
