using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Nexus.App.Converters
{
    /// <summary>
    /// Compares bound value to ConverterParameter (string).
    /// Returns Visible if equal, Collapsed otherwise.
    /// </summary>
    public class ViewToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
