using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Models;
using RoclandGuardiaRelevo.Mobile.Services;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    // Datos generales
    [ObservableProperty] private string bienvenida = string.Empty;
    [ObservableProperty] private string iniciales = string.Empty;
    [ObservableProperty] private string fechaHoy = DateTime.Today.ToString("dddd d 'de' MMMM");
    [ObservableProperty] private string turno = string.Empty;

    // Estado del rondín disponible en este momento
    [ObservableProperty] private string tipoRondinDisponible = string.Empty;
    [ObservableProperty] private bool rondinDisponible;
    [ObservableProperty] private bool yaRealizado;
    [ObservableProperty] private string textoBotonAccion = "No hay rondín disponible";

    // Estado de los 4 rondines del día (para mostrar resumen)
    [ObservableProperty] private bool amsEnviado;
    [ObservableProperty] private bool bmeEnviado;
    [ObservableProperty] private bool avsEnviado;
    [ObservableProperty] private bool bveEnviado;

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

            // Actualizar banderas de resumen del día
            AmsEnviado = RondinFlagsService.EstaEnviado("AMS");
            BmeEnviado = RondinFlagsService.EstaEnviado("BME");
            AvsEnviado = RondinFlagsService.EstaEnviado("AVS");
            BveEnviado = RondinFlagsService.EstaEnviado("BVE");

            var horaActual = TimeOnly.FromDateTime(DateTime.Now);
            var tipoRondin = AppConstants.ObtenerTipoRondinSegunHoraYTurno(_auth.Turno, horaActual);
            var existeEnVentana = !string.IsNullOrEmpty(tipoRondin);

            YaRealizado = false;

            // Verificación con bandera local (rápido, sin llamada al API)
            // En ModoPruebas = true se omite para poder probar múltiples veces
            if (existeEnVentana && !AppConstants.ModoPruebas)
            {
                YaRealizado = RondinFlagsService.EstaEnviado(tipoRondin);
            }

            if (YaRealizado)
            {
                RondinDisponible = false;
                TipoRondinDisponible = AppConstants.DescripcionTipoRondin(tipoRondin);
                TextoBotonAccion = "Rondín ya realizado";
            }
            else if (existeEnVentana)
            {
                RondinDisponible = true;
                TipoRondinDisponible = AppConstants.DescripcionTipoRondin(tipoRondin);
                TextoBotonAccion = "Realizar rondín ahora";
            }
            else
            {
                RondinDisponible = false;
                TipoRondinDisponible = "Ninguno (fuera de ventana horaria)";
                TextoBotonAccion = "No disponible";
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
            await Shell.Current.DisplayAlertAsync("Información", "Ya realizaste este rondín hoy.", "OK");
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

        await Shell.Current.GoToAsync($"RondinPage?tipoRondin={tipoRondin}");
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