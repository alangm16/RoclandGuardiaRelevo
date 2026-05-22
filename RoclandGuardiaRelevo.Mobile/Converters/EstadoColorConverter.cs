using System.Globalization;

namespace RoclandGuardiaRelevo.Mobile.Converters;

public class EstadoColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string estado = value as string ?? "";
        return estado switch
        {
            "Pendiente" => Color.FromArgb("#FF9800"),
            "EnCurso" => Color.FromArgb("#2196F3"),
            "Completado" => Color.FromArgb("#4CAF50"),
            "Incompleto" => Color.FromArgb("#F44336"),
            _ => Color.FromArgb("#9E9E9E")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}