using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KhokharumLaa.Converters
{
    /// <summary>
    /// Converts a boolean value to a System.Windows.Visibility value, but inverted.
    /// true -> Collapsed
    /// false -> Visible
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // If the boolean is true, we want to hide the element (Collapse it).
                // If it's false, we want to show it.
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            // Default to visible if the value is not a boolean.
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter is one-way, so we don't need to implement ConvertBack.
            throw new NotSupportedException();
        }
    }
}
