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

            // ── 1. Llamar al endpoint autoritativo ──────────────────────
            // Pasamos la fecha LOCAL del dispositivo para que el servidor
            // haga la conversión UTC correcta.
            var estadoDia = await _api.GetEstadoDiaAsync(DateTime.Today);

            // ── 2. Actualizar indicadores de estado del día ──────────────
            // El servidor ya sabe qué existe; no hacemos aritmética aquí.
            if (estadoDia != null && estadoDia.Rondines.Count > 0)
            {
                AmsEnviado = estadoDia.Rondines.FirstOrDefault(r => r.TipoRondin == "AMS")?.Existe ?? false;
                BmeEnviado = estadoDia.Rondines.FirstOrDefault(r => r.TipoRondin == "BME")?.Existe ?? false;
                AvsEnviado = estadoDia.Rondines.FirstOrDefault(r => r.TipoRondin == "AVS")?.Existe ?? false;
                BveEnviado = estadoDia.Rondines.FirstOrDefault(r => r.TipoRondin == "BVE")?.Existe ?? false;
            }
            else
            {
                // Sin red: dejar todo en false (estado desconocido)
                AmsEnviado = BmeEnviado = AvsEnviado = BveEnviado = false;
            }

            // ── 3. Determinar rondín disponible en este momento ──────────
            var horaActual = TimeOnly.FromDateTime(DateTime.Now);
            var tipoRondin = AppConstants.ObtenerTipoRondinSegunHoraYTurno(_auth.Turno, horaActual);
            var existeVentana = !string.IsNullOrEmpty(tipoRondin);

            // ── 4. ¿Ya lo hizo el guardia autenticado? ───────────────────
            // Usamos el campo YoLoHice que calculó el servidor.
            // Si no hay red, asumimos que NO (permite reintentar al volver conexión).
            bool guardiaYaEnvio = false;
            if (existeVentana && estadoDia != null)
            {
                guardiaYaEnvio = estadoDia.Rondines
                    .FirstOrDefault(r => r.TipoRondin == tipoRondin)
                    ?.YoLoHice ?? false;
            }

            YaRealizado = guardiaYaEnvio;

            // ── 5. Actualizar texto/estado del botón ─────────────────────
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