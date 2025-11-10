using Safe1.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Safe1.Views
{
    public class ProtectionModeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProtectionMode pm)
            {
                switch (pm)
                {
                    case ProtectionMode.NORMAL: return Brushes.Gray;
                    case ProtectionMode.LOCKED: return Brushes.Orange;
                    case ProtectionMode.ENCRYPTED: return Brushes.Green;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
