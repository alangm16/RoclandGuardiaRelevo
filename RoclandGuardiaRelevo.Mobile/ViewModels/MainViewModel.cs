using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Models;
using RoclandGuardiaRelevo.Mobile.Services;

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
    [ObservableProperty] private List<object> historialRelevos = new();

    private MiActivoResponse? miActivo;  // Cambiado de RelevoHoyResponse a MiActivoResponse

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
            // 1. Obtener el relevo activo completo (incluye participanteId, rol, y el objeto Relevo)
            miActivo = await _api.GetMiRelevoActivoAsync();

            if (miActivo == null)
            {
                SinRelevo = true;
                HayRelevo = false;
                return;
            }

            var relevo = miActivo.Relevo; // El objeto RelevoHoyResponse
            SinRelevo = false;
            HayRelevo = true;

            // Datos del relevo
            TurnoNombre = relevo.NombreTurno;
            RelevoTitulo = $"Relevo del {relevo.Fecha:dd/MM/yyyy}";
            EstadoBadge = relevo.Estado switch
            {
                "Pendiente" => "⏳ Pendiente",
                "EnCurso" => "🟢 En curso",
                "Completado" => "✅ Completado",
                "Incompleto" => "⚠️ Incompleto",
                _ => relevo.Estado
            };

            // Mostrar datos de ambos guardias
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

            // Calcular progreso (solo para mi rol)
            var miParticipante = miActivo.Rol == "Saliente" ? relevo.Saliente : relevo.Entrante;
            if (miParticipante != null)
            {
                int total = miParticipante.TotalOk + miParticipante.TotalNoOk;
                int contestados = total;
                // Obtener el total de puntos del checklist dinámicamente
                var checklist = await _api.GetChecklistPuntosAsync();
                int totalPuntos = checklist?.Sum(c => c.Puntos.Count) ?? 10;
                double progreso = totalPuntos > 0 ? (double)contestados / totalPuntos : 0;
                ProgresoValor = progreso;
                ProgresoTexto = $"{contestados}/{totalPuntos}";
            }

            // Definir texto del botón de acción según estado de mi participante
            TextoBotonAccion = miParticipante?.Estado switch
            {
                "Pendiente" => "Iniciar turno",
                "EnCurso" => "Continuar rondín",
                "Completado" => "Ver resumen",
                "Expirado" => "Turno expirado",
                _ => "Ver detalles"
            };
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo cargar el relevo.", "OK");
        }
        finally
        {
            EstaCargando = false;
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
                var exito = await _api.IniciarParticipanteAsync(miActivo.ParticipanteId);
                if (exito)
                    await CargarRelevoAsync();
                else
                    await Shell.Current.DisplayAlert("Error", "No se pudo iniciar el turno. Verifica tu horario.", "OK");
                break;
            case "EnCurso":
                await Shell.Current.GoToAsync($"RondinPage?participanteId={miActivo.ParticipanteId}&rol={miActivo.Rol}&relevoId={miActivo.Relevo.RelevoId}");
                break;
            case "Completado":
                await Shell.Current.DisplayAlert("Rondín completado", $"Finalizaste tu turno el {miParticipante.FechaFin?.ToString("HH:mm")}", "OK");
                break;
            default:
                await Shell.Current.DisplayAlert("Información", $"Estado actual: {miParticipante.Estado}", "OK");
                break;
        }
    }

    [RelayCommand]
    private async Task VerDetalleHistorialAsync(object? selectedItem)
    {
        if (selectedItem is HistorialRelevoItem item)
        {
            await Shell.Current.DisplayAlert("Detalle", $"Relevo del {item.Fecha}\nEstado: {item.Estado}", "OK");
        }
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
        "Pendiente" => "⏳ Pendiente",
        "EnCurso" => "🟢 En curso",
        "Completado" => "✅ Completado",
        "Expirado" => "⌛ Expirado",
        _ => estado
    };
}