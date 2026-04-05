using System.Windows.Data;

namespace LidarIndoorNavigation.Helpers
{
    internal class BoolToOpenClosedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "OPEN" : "CLOSED";
            }
            return "CLOSED";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue.Equals("OPEN", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
