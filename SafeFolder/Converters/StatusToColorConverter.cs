using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SafeFolder.Converters
{
    /// <summary>
    /// Converts a folder status string ("Locked", "Normal") into a specific color Brush.
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Locked":
                        return new SolidColorBrush(Colors.OrangeRed);
                    case "Normal":
                        return new SolidColorBrush(Colors.Green);
                    default:
                        return new SolidColorBrush(Colors.Black);
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter does not support converting back.
            throw new NotImplementedException();
        }
    }
}
