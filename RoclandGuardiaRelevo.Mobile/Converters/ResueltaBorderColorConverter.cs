using System.Globalization;

namespace RoclandGuardiaRelevo.Mobile.Converters;

public class ResueltaBorderColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? Color.FromArgb("#C8E6C9") : Color.FromArgb("#FFCDD2");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}