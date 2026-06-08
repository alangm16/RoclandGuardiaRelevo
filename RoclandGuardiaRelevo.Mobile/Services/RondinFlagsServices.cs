namespace RoclandGuardiaRelevo.Mobile.Services;

/// <summary>
/// Registra localmente si cada guardia ya envió cada tipo de rondín en el día actual.
/// La clave incluye IdGuardia + TipoRondin + Fecha, por lo que:
///   ─ Dos guardias en el mismo dispositivo tienen flags independientes.
///   ─ Al cambiar de día, los flags caducan automáticamente.
///   ─ Reinstalar la app borra Preferences, lo que permite volver a enviar (ideal en pruebas).
/// En ModoPruebas = true el MainViewModel/RondinViewModel omiten el chequeo de servidor,
/// usando únicamente estos flags locales.
/// </summary>
public static class RondinFlagsService
{
    // Clave única: guardia + tipo + fecha
    private static string Key(int idGuardia, string tipo) =>
        $"rondin_{idGuardia}_{tipo}_{DateTime.Today:yyyyMMdd}";

    /// <summary>¿Este guardia ya envió este tipo de rondín hoy (en este dispositivo)?</summary>
    public static bool EstaEnviado(int idGuardia, string tipo) =>
        Preferences.Default.Get(Key(idGuardia, tipo), false);

    /// <summary>Marca que este guardia ya envió este tipo de rondín hoy.</summary>
    public static void MarcarEnviado(int idGuardia, string tipo) =>
        Preferences.Default.Set(Key(idGuardia, tipo), true);

    /// <summary>
    /// Limpia todos los flags del guardia para el día actual.
    /// Llamar desde el panel de pruebas para poder volver a enviar sin reinstalar.
    /// En producción el reset ocurre automáticamente al cambiar de fecha.
    /// </summary>
    public static void ResetearGuardia(int idGuardia)
    {
        foreach (var tipo in new[] { "AMS", "BME", "AVS", "BVE" })
            Preferences.Default.Remove(Key(idGuardia, tipo));
    }

    /// <summary>
    /// Limpia los flags de TODOS los guardias para el día actual.
    /// Solo para uso en el panel de pruebas.
    /// </summary>
    public static void ResetearTodos()
    {
        // Recorremos los guardias conocidos; no hay un ID fijo, así que borramos
        // todas las claves que coincidan con el patrón del día actual.
        // Preferences no soporta glob, así que usamos un conjunto conocido de IDs recientes.
        // Como workaround más seguro: guardar un Set de IDs que han marcado algo hoy.
        var idsHoy = Preferences.Default.Get($"guardias_activos_{DateTime.Today:yyyyMMdd}", "");
        if (!string.IsNullOrEmpty(idsHoy))
        {
            foreach (var idStr in idsHoy.Split(','))
            {
                if (int.TryParse(idStr, out int id))
                    ResetearGuardia(id);
            }
        }
        Preferences.Default.Remove($"guardias_activos_{DateTime.Today:yyyyMMdd}");
    }

    /// <summary>
    /// Registra el IdGuardia en la lista de guardias activos del día,
    /// para que ResetearTodos() sepa qué claves limpiar.
    /// Llamar al mismo tiempo que MarcarEnviado.
    /// </summary>
    public static void RegistrarGuardiaActivo(int idGuardia)
    {
        var key = $"guardias_activos_{DateTime.Today:yyyyMMdd}";
        var actuales = Preferences.Default.Get(key, "");
        var lista = actuales.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
        var idStr = idGuardia.ToString();
        if (!lista.Contains(idStr))
        {
            lista.Add(idStr);
            Preferences.Default.Set(key, string.Join(",", lista));
        }
    }
}