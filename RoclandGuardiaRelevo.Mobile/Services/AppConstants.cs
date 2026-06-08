namespace RoclandGuardiaRelevo.Mobile.Services;

public static class AppConstants
{

    public const string BaseUrlAndroid = "https://dev.rocland.mx";
    public const string BaseUrlWindows = "https://localhost:7244";
    public const string CodigoProyectoGuardiaRelevo = "guardia-relevo";
    public const string PlataformaMobile = "Mobile";

    // Ventanas horarias para cada tipo de rondín (hora local del dispositivo)
    //public static readonly TimeOnly HoraInicioAMS = new(6, 0);
    //public static readonly TimeOnly HoraFinAMS = new(7, 0);
    //public static readonly TimeOnly HoraInicioBME = new(7, 0);
    //public static readonly TimeOnly HoraFinBME = new(8, 0);
    //public static readonly TimeOnly HoraInicioAVS = new(18, 0);
    //public static readonly TimeOnly HoraFinAVS = new(19, 0);
    //public static readonly TimeOnly HoraInicioBVE = new(19, 0);
    //public static readonly TimeOnly HoraFinBVE = new(20, 0);

    public const bool ModoPruebas = false;

    public static readonly TimeOnly HoraInicioAMS = new(10, 35);
    public static readonly TimeOnly HoraFinAMS = new(10, 39);
    public static readonly TimeOnly HoraInicioBME = new(10, 43);
    public static readonly TimeOnly HoraFinBME = new(10, 47);

    public static readonly TimeOnly HoraInicioAVS = new(10, 50);
    public static readonly TimeOnly HoraFinAVS = new(10, 54);
    public static readonly TimeOnly HoraInicioBVE = new(10, 58);
    public static readonly TimeOnly HoraFinBVE = new(11, 02);

    /// <summary>
    /// [OBSOLETO — No usar en código nuevo]
    /// La validación de ventana horaria ahora la hace el servidor vía
    /// SP_ROCLAND_RELEVO_ESTADO_DIA / GetEstadoDiaAsync.
    /// Este método se mantiene solo por compatibilidad con ModoPruebas.
    /// </summary>
    [Obsolete("Usar GetEstadoDiaAsync en ApiService. Este método solo existe para ModoPruebas.")]
    public static bool EstaEnVentanaActual(string tipo, DateTime fechaHoraLocal)
    {
        var hora = TimeOnly.FromDateTime(fechaHoraLocal);
        return tipo switch
        {
            "AMS" => hora >= HoraInicioAMS && hora < HoraFinAMS,
            "BME" => hora >= HoraInicioBME && hora < HoraFinBME,
            "AVS" => hora >= HoraInicioAVS && hora < HoraFinAVS,
            "BVE" => hora >= HoraInicioBVE && hora < HoraFinBVE,
            _ => false
        };
    }

    // Guardia A (diurno) puede hacer AMS y AVS; Guardia B (nocturno) puede hacer BME y BVE
    // El backend devuelve el turno del guardia: "Diurno" o "Nocturno"
    public static string ObtenerTipoRondinSegunHoraYTurno(string turnoGuardia, TimeOnly horaActual)
    {
        if (turnoGuardia == "Diurno")
        {
            // Matutino: entrante (BME)
            if (horaActual >= HoraInicioBME && horaActual < HoraFinBME)
                return "BME";
            // Vespertino: saliente (AVS)
            if (horaActual >= HoraInicioAVS && horaActual < HoraFinAVS)
                return "AVS";
        }
        else if (turnoGuardia == "Nocturno")
        {
            // Matutino: saliente (AMS)
            if (horaActual >= HoraInicioAMS && horaActual < HoraFinAMS)
                return "AMS";
            // Vespertino: entrante (BVE)
            if (horaActual >= HoraInicioBVE && horaActual < HoraFinBVE)
                return "BVE";
        }
        return string.Empty;
    }

    // Descripción amigable para mostrar al usuario
    public static string DescripcionTipoRondin(string tipo)
    {
        return tipo switch
        {
            "AMS" => "Matutino Saliente (6:00-7:00)",
            "BME" => "Matutino Entrante (7:00-8:00)",
            "AVS" => "Vespertino Saliente (18:00-19:00)",
            "BVE" => "Vespertino Entrante (19:00-20:00)",
            _ => "Desconocido"
        };
    }
}