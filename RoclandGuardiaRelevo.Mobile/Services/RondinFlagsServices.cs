namespace RoclandGuardiaRelevo.Mobile.Services;

/// <summary>
/// Registra localmente si cada tipo de rondín ya fue enviado en el día actual.
/// Las claves incluyen la fecha, por lo que se renuevan automáticamente al día siguiente.
/// En ModoPruebas = true estas verificaciones se omiten para facilitar el desarrollo.
/// </summary>
public static class RondinFlagsService
{
    private static string Key(string tipo) =>
        $"rondin_enviado_{tipo}_{DateTime.Today:yyyyMMdd}";

    /// <summary>¿Ya se envió este tipo de rondín hoy?</summary>
    public static bool EstaEnviado(string tipo) =>
        Preferences.Default.Get(Key(tipo), false);

    /// <summary>Marca el tipo como enviado para hoy.</summary>
    public static void MarcarEnviado(string tipo) =>
        Preferences.Default.Set(Key(tipo), true);

    /// <summary>
    /// Limpia todas las banderas del día actual.
    /// Útil solo en ModoPruebas o para debugging; en producción el reset es automático por fecha.
    /// </summary>
    public static void ResetearTodos()
    {
        foreach (var tipo in new[] { "AMS", "BME", "AVS", "BVE" })
            Preferences.Default.Remove(Key(tipo));
    }
}