using System.Globalization;

namespace RoclandGuardiaRelevo.Mobile.Converters;

public class BoolToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "check_circle.png" : "cancel_circle.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}