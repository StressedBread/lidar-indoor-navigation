using System.Windows.Data;

namespace LidarIndoorNavigation.Helpers
{
    public class BoolToOnOffConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "ON" : "OFF";
            }
            return "OFF";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue.Equals("ON", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
