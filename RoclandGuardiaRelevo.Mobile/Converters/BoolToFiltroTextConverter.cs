using System.Globalization;

namespace RoclandGuardiaRelevo.Mobile.Converters;

public class BoolToFiltroTextConverter : IValueConverter
{
    // Cuando MostrarSoloAbiertas = true → "Mostrar todas"
    // Cuando MostrarSoloAbiertas = false → "Solo abiertas"
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "Mostrar todas" : "Solo abiertas";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}