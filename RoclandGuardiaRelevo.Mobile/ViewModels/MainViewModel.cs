using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Models;
using RoclandGuardiaRelevo.Mobile.Services;
using System.Collections.ObjectModel;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    [ObservableProperty] private string bienvenida = string.Empty;
    [ObservableProperty] private string iniciales = string.Empty;
    [ObservableProperty] private string fechaHoy = DateTime.Today.ToString("dddd d 'de' MMMM");
    [ObservableProperty] private string nombreRol = string.Empty;

    [ObservableProperty] private bool sinRelevo = true;
    [ObservableProperty] private bool hayRelevo = false;

    [ObservableProperty] private string turnoNombre = string.Empty;
    [ObservableProperty] private string relevoTitulo = string.Empty;
    [ObservableProperty] private string estadoBadge = string.Empty;

    [ObservableProperty] private string inicialSaliente = "?";
    [ObservableProperty] private string nombreSaliente = "Sin asignar";
    [ObservableProperty] private string estadoSaliente = string.Empty;

    [ObservableProperty] private string inicialEntrante = "?";
    [ObservableProperty] private string nombreEntrante = "Sin asignar";
    [ObservableProperty] private string estadoEntrante = string.Empty;

    [ObservableProperty] private string progresoTexto = "0%";
    [ObservableProperty] private double progresoValor = 0;

    [ObservableProperty] private string textoBotonAccion = "Cargando...";

    // Historial tipado correctamente
    [ObservableProperty] private ObservableCollection<RelevoListResponse> historial = new();

    private MiActivoResponse? miActivo;

    public MainViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;

        Titulo = "Panel Principal";
        Bienvenida = $"¡Hola, {_auth.NombreGuardia}!";
        Iniciales = ObtenerIniciales(_auth.NombreGuardia);
        NombreRol = "Guardia";
    }

    [RelayCommand]
    public async Task CargarRelevoAsync()
    {
        if (EstaCargando) return;
        EstaCargando = true;

        try
        {
            // 1. Obtener el relevo activo completo
            miActivo = await _api.GetMiRelevoActivoAsync();

            if (miActivo == null)
            {
                SinRelevo = true;
                HayRelevo = false;
                return;
            }

            var relevo = miActivo.Relevo;
            SinRelevo = false;
            HayRelevo = true;

            TurnoNombre = relevo.NombreTurno;
            RelevoTitulo = $"Relevo del {relevo.Fecha:dd/MM/yyyy}";
            EstadoBadge = relevo.Estado switch
            {
                "Pendiente" => "Pendiente",
                "EnCurso" => "En curso",
                "Completado" => "Completado",
                "Incompleto" => "Incompleto",
                _ => relevo.Estado
            };

            if (relevo.Saliente != null)
            {
                NombreSaliente = relevo.Saliente.NombreGuardia;
                InicialSaliente = ObtenerIniciales(relevo.Saliente.NombreGuardia);
                EstadoSaliente = FormatearEstado(relevo.Saliente.Estado);
            }
            if (relevo.Entrante != null)
            {
                NombreEntrante = relevo.Entrante.NombreGuardia;
                InicialEntrante = ObtenerIniciales(relevo.Entrante.NombreGuardia);
                EstadoEntrante = FormatearEstado(relevo.Entrante.Estado);
            }

            var miParticipante = miActivo.Rol == "Saliente" ? relevo.Saliente : relevo.Entrante;
            if (miParticipante != null)
            {
                int total = miParticipante.TotalOk + miParticipante.TotalNoOk;
                int contestados = total;
                var checklist = await _api.GetChecklistPuntosAsync();
                int totalPuntos = checklist?.Sum(c => c.Puntos.Count) ?? 10;
                double progreso = totalPuntos > 0 ? (double)contestados / totalPuntos : 0;
                ProgresoValor = progreso;
                ProgresoTexto = $"{contestados}/{totalPuntos}";
            }

            TextoBotonAccion = miParticipante?.Estado switch
            {
                "Pendiente" => "Iniciar turno",
                "EnCurso" => "Continuar rondín",
                "Completado" => "Ver resumen",
                "Expirado" => "Turno expirado",
                _ => "Ver detalles"
            };

            // Cargar historial
            await CargarHistorialAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", "No se pudo cargar el relevo.", "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task CargarHistorialAsync()
    {
        var resultado = await _api.GetHistorialRelevosAsync(1, 10);
        if (resultado != null)
        {
            Historial = new ObservableCollection<RelevoListResponse>(resultado.Items);
        }
    }

    [RelayCommand]
    private async Task AccionPrincipalAsync()
    {
        if (miActivo == null) return;

        var miParticipante = miActivo.Rol == "Saliente"
            ? miActivo.Relevo.Saliente
            : miActivo.Relevo.Entrante;
        if (miParticipante == null) return;

        switch (miParticipante.Estado)
        {
            case "Pendiente":
                var iniciarResult = await _api.IniciarRondinAsync();
                if (iniciarResult != null && iniciarResult.Exito)
                    await CargarRelevoAsync();
                else
                    await Shell.Current.DisplayAlertAsync("Error", iniciarResult?.Mensaje ?? "No se pudo iniciar el rondín.", "OK");
                break;
            case "EnCurso":
                await Shell.Current.GoToAsync($"RondinPage?participanteId={miActivo.ParticipanteId}&rol={miActivo.Rol}&relevoId={miActivo.Relevo.RelevoId}");
                break;
            case "Completado":
                await Shell.Current.DisplayAlertAsync("Rondín completado", $"Finalizaste tu turno el {miParticipante.FechaFin?.ToString("HH:mm")}", "OK");
                break;
            default:
                await Shell.Current.DisplayAlertAsync("Información", $"Estado actual: {miParticipante.Estado}", "OK");
                break;
        }
    }

    [RelayCommand]
    private async Task VerDetalleHistorialAsync(RelevoListResponse? item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"RelevoDetallePage?relevoId={item.Id}");
    }

    private string ObtenerIniciales(string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto)) return "?";
        var partes = nombreCompleto.Trim().Split(' ');
        if (partes.Length >= 2)
            return $"{partes[0][0]}{partes[1][0]}".ToUpper();
        return nombreCompleto[..1].ToUpper();
    }

    private string FormatearEstado(string estado) => estado switch
    {
        "Pendiente" => "Pendiente",
        "EnCurso" => "En curso",
        "Completado" => "Completado",
        "Expirado" => "Expirado",
        _ => estado
    };

    [RelayCommand]
    private async Task CerrarSesionAsync()
    {
        bool respuesta = await Shell.Current.DisplayAlertAsync("Cerrar sesión", "¿Estás seguro que deseas salir de tu cuenta?", "Sí, salir", "Cancelar");
        if (respuesta)
        {
            _auth.CerrarSesion();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}