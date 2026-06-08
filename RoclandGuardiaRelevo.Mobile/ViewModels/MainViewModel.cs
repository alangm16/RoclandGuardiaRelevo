using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Models;
using RoclandGuardiaRelevo.Mobile.Services;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    // ── Cabecera ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string bienvenida = string.Empty;
    [ObservableProperty] private string iniciales = string.Empty;
    [ObservableProperty] private string fechaHoy = DateTime.Today.ToString("dddd d 'de' MMMM");
    [ObservableProperty] private string turno = string.Empty;

    // ── Rondín disponible en este momento ────────────────────────────────────
    [ObservableProperty] private string tipoRondinDisponible = string.Empty;
    [ObservableProperty] private bool rondinDisponible;
    [ObservableProperty] private bool yaRealizado;
    [ObservableProperty] private string textoBotonAccion = "No hay rondín disponible";

    // ── Estado visual de los 4 rondines del día (siempre desde servidor) ─────
    // Estos reflejan si el rondín existe en BD, independientemente del guardia.
    // Sirven para que el guardia ENTRANTE vea si el SALIENTE ya hizo su parte.
    [ObservableProperty] private bool amsEnviado;
    [ObservableProperty] private bool bmeEnviado;
    [ObservableProperty] private bool avsEnviado;
    [ObservableProperty] private bool bveEnviado;

    // ── Modo pruebas ─────────────────────────────────────────────────────────
    /// <summary>
    /// Expone el flag de AppConstants al binding del XAML.
    /// Controla la visibilidad del panel de depuración.
    /// </summary>
    public bool EsModoPruebas => AppConstants.ModoPruebas;

    // ──────────────────────────────────────────────────────────────────────────

    public MainViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        Titulo = "Panel Principal";
        Turno = _auth.Turno;
    }

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        if (EstaCargando) return;
        EstaCargando = true;

        try
        {
            Bienvenida = $"¡Hola, {_auth.NombreGuardia}!";
            Iniciales = ObtenerIniciales(_auth.NombreGuardia);

            var hoy = DateTime.Today;
            List<string> rondinesEnBd = new();

            // Declarado FUERA del try para que exista en todo el método
            List<ChecklistResumenDto> historialHoy = new();

            try
            {
                var resultado = await _api.GetHistorialAsync(idGuardia: null, desde: hoy, hasta: hoy);
                if (resultado != null) historialHoy = resultado;

                // MAGIA AQUÍ: Solo tomamos en cuenta los rondines cuyo registro coincida 
                // con las horas que tienes actualmente configuradas en AppConstants
                rondinesEnBd = historialHoy
                    .Where(h => AppConstants.EstaEnVentanaActual(h.TipoRondin, h.FechaHoraLocal))
                    .Select(h => h.TipoRondin)
                    .Distinct()
                    .ToList();
            }
            catch { /* Falla silenciosa si no hay red */ }

            AmsEnviado = rondinesEnBd.Contains("AMS");
            BmeEnviado = rondinesEnBd.Contains("BME");
            AvsEnviado = rondinesEnBd.Contains("AVS");
            BveEnviado = rondinesEnBd.Contains("BVE");

            var horaActual = TimeOnly.FromDateTime(DateTime.Now);
            var tipoRondin = AppConstants.ObtenerTipoRondinSegunHoraYTurno(_auth.Turno, horaActual);
            var existeVentana = !string.IsNullOrEmpty(tipoRondin);

            bool guardiaYaEnvio = false;
            if (existeVentana)
            {
                // Verificamos si TÚ ya enviaste este rondín DENTRO de las horas configuradas
                guardiaYaEnvio = historialHoy.Any(h =>
                    h.IdGuardia == _auth.IdGuardia &&
                    h.TipoRondin == tipoRondin &&
                    AppConstants.EstaEnVentanaActual(h.TipoRondin, h.FechaHoraLocal));
            }

            YaRealizado = guardiaYaEnvio;

            if (!existeVentana)
            {
                RondinDisponible = false;
                TipoRondinDisponible = "Ninguno (fuera de ventana horaria)";
                TextoBotonAccion = "No disponible";
            }
            else if (guardiaYaEnvio)
            {
                RondinDisponible = false;
                TipoRondinDisponible = AppConstants.DescripcionTipoRondin(tipoRondin);
                TextoBotonAccion = "Rondín ya completado en este horario ✓";
            }
            else
            {
                RondinDisponible = true;
                TipoRondinDisponible = AppConstants.DescripcionTipoRondin(tipoRondin);
                TextoBotonAccion = "Realizar rondín ahora";
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task AccionPrincipalAsync()
    {
        if (YaRealizado)
        {
            await Shell.Current.DisplayAlertAsync("Información", "Ya realizaste este rondín el día de hoy según la base de datos.", "OK");
            return;
        }

        if (!RondinDisponible)
        {
            await Shell.Current.DisplayAlertAsync("Fuera de horario", "No hay rondín disponible en este momento.", "OK");
            return;
        }

        var horaActual = TimeOnly.FromDateTime(DateTime.Now);
        var tipoRondin = AppConstants.ObtenerTipoRondinSegunHoraYTurno(_auth.Turno, horaActual);
        if (string.IsNullOrEmpty(tipoRondin))
        {
            await Shell.Current.DisplayAlertAsync("Error", "No se pudo determinar el tipo de rondín.", "OK");
            return;
        }

        // --- NUEVA LÓGICA DE VALIDACIÓN ENTRE GUARDIAS ---
        // Si el guardia actual es entrante (BME o BVE), verificamos si el saliente cumplió
        if (tipoRondin == "BME" && !AmsEnviado)
        {
            bool continuar = await Shell.Current.DisplayAlertAsync("Aviso de Relevo",
                "El guardia saliente no registró su rondín matutino. No se podrán comparar las anomalías automáticamente. ¿Deseas generar tu rondín de todos modos?",
                "Sí, continuar", "Cancelar");
            if (!continuar) return;
        }
        else if (tipoRondin == "BVE" && !AvsEnviado)
        {
            bool continuar = await Shell.Current.DisplayAlertAsync("Aviso de Relevo",
                "El guardia saliente no registró su rondín vespertino. No se podrán comparar las anomalías automáticamente. ¿Deseas generar tu rondín de todos modos?",
                "Sí, continuar", "Cancelar");
            if (!continuar) return;
        }
        // --------------------------------------------------

        await Shell.Current.GoToAsync($"RondinPage?tipoRondin={tipoRondin}");
    }

    /// <summary>
    /// Solo disponible en ModoPruebas. Limpia los flags locales de TODOS los guardias
    /// para el día actual, permitiendo volver a enviar sin necesidad de reinstalar.
    /// Los datos previos siguen en la BD (comportamiento esperado en pruebas).
    /// </summary>
    [RelayCommand]
    private async Task ReiniciarFlagsPruebasAsync()
    {
        if (!AppConstants.ModoPruebas) return;

        bool confirmar = await Shell.Current.DisplayAlertAsync(
            "Reiniciar flags de prueba",
            "Esto borrará el registro local de rondines enviados hoy, permitiéndote volver a enviarlos.\n\nLos datos en la base de datos NO se borran.",
            "Sí, reiniciar", "Cancelar");

        if (!confirmar) return;

        RondinFlagsService.ResetearTodos();
        await CargarDatosAsync(); // Refrescar UI
        await Shell.Current.DisplayAlertAsync("Listo", "Flags locales reiniciados. Ya puedes volver a enviar rondines.", "OK");
    }

    [RelayCommand]
    private async Task CerrarSesionAsync()
    {
        bool respuesta = await Shell.Current.DisplayAlertAsync("Cerrar sesión", "¿Estás seguro?", "Sí", "Cancelar");
        if (respuesta)
        {
            _auth.CerrarSesion();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    private static string ObtenerIniciales(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "?";
        var partes = nombre.Trim().Split(' ');
        return partes.Length >= 2
            ? $"{partes[0][0]}{partes[1][0]}".ToUpper()
            : nombre[..1].ToUpper();
    }
}