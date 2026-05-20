using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Models;
using RoclandGuardiaRelevo.Mobile.Services;
using RoclandGuardiaRelevo.Mobile.Views;
using System.Collections.ObjectModel;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class RondinViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;
    private int participanteId;
    private string miRol = string.Empty;
    private int relevoId;
    private List<PuntoChecklistVm> todosLosPuntos = new();

    [ObservableProperty] private string subtitulo = string.Empty;
    [ObservableProperty] private string instruccionTexto = string.Empty;
    [ObservableProperty] private string progresoTexto = "0%";
    [ObservableProperty] private double progresoValor = 0;
    [ObservableProperty] private string novedades = string.Empty;
    [ObservableProperty] private bool puedeEnviar;
    [ObservableProperty] private Color colorBoton = Colors.Gray;
    [ObservableProperty] private ObservableCollection<CategoriaChecklistVm> categorias = new();

    public RondinViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
    }

    public async Task CargarChecklistAsync(int participanteId, string rol, int relevoId)
    {
        this.participanteId = participanteId;
        miRol = rol;
        this.relevoId = relevoId;

        Subtitulo = rol == "Saliente" ? "Entrega de turno" : "Recepción de turno";
        InstruccionTexto = rol == "Saliente"
            ? "Verifica cada punto y marca SÍ o NO. Si marcas NO, se te pedirá un comentario y foto (opcional)."
            : "Revisa cada punto con el guardia saliente. Marca NO si hay anomalía.";

        EstaCargando = true;
        try
        {
            var puntosBackend = await _api.GetChecklistPuntosAsync();
            if (puntosBackend == null) throw new Exception("No se pudo cargar el checklist");

            var respuestasGuardadas = await _api.GetRespuestasPorParticipanteAsync(participanteId);
            var dictRespuestas = respuestasGuardadas?.ToDictionary(r => r.PuntoId) ?? new();

            todosLosPuntos.Clear();
            var nuevasCategorias = new ObservableCollection<CategoriaChecklistVm>();
            foreach (var cat in puntosBackend)
            {
                var catVm = new CategoriaChecklistVm { Nombre = cat.Categoria };
                foreach (var punto in cat.Puntos)
                {
                    var vm = new PuntoChecklistVm
                    {
                        Id = punto.Id,
                        Nombre = punto.Nombre,
                        Descripcion = punto.Descripcion,
                        Orden = punto.Orden
                    };
                    if (dictRespuestas.TryGetValue(punto.Id, out var existente))
                    {
                        vm.Respuesta = existente.Respuesta;
                        vm.Comentario = existente.Comentario;
                        vm.ActualizarApariencia(existente.Respuesta);
                    }
                    catVm.Puntos.Add(vm);
                    todosLosPuntos.Add(vm);
                }
                nuevasCategorias.Add(catVm);
            }
            Categorias = nuevasCategorias;
            ActualizarProgreso();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void ActualizarProgreso()
    {
        int respondidos = todosLosPuntos.Count(p => p.Respuesta.HasValue);
        int total = todosLosPuntos.Count;
        ProgresoTexto = $"{respondidos}/{total}";
        ProgresoValor = total > 0 ? (double)respondidos / total : 0;
        PuedeEnviar = respondidos == total;
        ColorBoton = PuedeEnviar ? Color.FromArgb("#4CAF50") : Colors.Gray;
    }

    public async Task<bool> GuardarRespuestaAsync(int puntoId, bool respuesta, string? comentario)
    {
        var request = new GuardarRespuestaRequest
        {
            PuntoId = puntoId,
            Respuesta = respuesta,
            Comentario = comentario
        };
        var exito = await _api.GuardarRespuestaAsync(participanteId, request);
        if (exito)
        {
            var punto = todosLosPuntos.First(p => p.Id == puntoId);
            punto.Respuesta = respuesta;
            punto.Comentario = comentario;
            punto.ActualizarApariencia(respuesta);
            ActualizarProgreso();
        }
        return exito;
    }

    public async Task CrearIncidenciaAsync(int puntoId, string comentario, string? fotoBase64, string? mimeType)
    {
        var request = new CrearIncidenciaRequest
        {
            RelevoId = relevoId,
            PuntoId = puntoId,
            TipoOrigen = "NoOk",
            Descripcion = comentario,
            FotoBase64 = fotoBase64,
            MimeType = mimeType
        };
        await _api.CrearIncidenciaAsync(request);
    }

    [RelayCommand]
    private async Task EnviarRondinAsync()
    {
        if (!PuedeEnviar)
        {
            await Shell.Current.DisplayAlert("Incompleto", "Responde todos los puntos antes de finalizar.", "OK");
            return;
        }

        var firmaVm = new FirmaViewModel();
        var firmaPage = new FirmaPage(firmaVm);

        // 1. Creamos un "semáforo" para obligar a MAUI a esperar a que la ventana se cierre
        var tcs = new TaskCompletionSource<bool>();
        firmaPage.Disappearing += (s, e) => tcs.TrySetResult(true);

        // Abrimos la pantalla modal
        await Shell.Current.Navigation.PushModalAsync(firmaPage);

        // 2. DETENEMOS LA EJECUCIÓN AQUÍ hasta que el semáforo dé luz verde (cuando desaparezca la pantalla)
        await tcs.Task;

        // Cuando la pantalla se cierra, ahora sí evaluamos si firmó o canceló
        if (!firmaVm.FirmaCompletada)
            return;

        // 3. Bloque Try-Catch para diagnosticar o atrapar errores al crear la petición
        try
        {
            // Alerta de diagnóstico temporal (puedes borrarla cuando veas que sí funciona)
            await Shell.Current.DisplayAlert("Progreso móvil", "Enviando datos al servidor...", "OK");

            var respuestas = todosLosPuntos.Select(p => new GuardarRespuestaRequest
            {
                PuntoId = p.Id,
                Respuesta = p.Respuesta!.Value,
                Comentario = p.Comentario
            }).ToList();

            var cerrarRequest = new CerrarParticipanteRequest
            {
                FirmaBase64 = Convert.ToBase64String(firmaVm.FirmaBytes!),
                Observaciones = Novedades,
                Respuestas = respuestas
            };

            var exito = await _api.CerrarParticipanteAsync(participanteId, cerrarRequest);

            if (exito)
            {
                await Shell.Current.DisplayAlert("Éxito", "Rondín finalizado correctamente.", "OK");
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "El servidor rechazó el cierre del rondín.", "OK");
            }
        }
        catch (Exception ex)
        {
            // 🔥 CAPTURA EL ERROR OCULTO Y MUÉSTRALO EN PANTALLA 🔥
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Error Crítico de Envío", $"Excepción: {ex.Message}\n\nDetalle: {ex.InnerException?.Message}", "OK");
            });
        }
    }

    [RelayCommand]
    private async Task ResponderSiAsync(PuntoChecklistVm punto)
    {
        if (punto.Respuesta == true) return;
        await GuardarRespuestaAsync(punto.Id, true, null);
    }

    [RelayCommand]
    private async Task ResponderNoAsync(PuntoChecklistVm punto)
    {
        if (punto.Respuesta == false) return;

        var popup = new IncidenciaPopup(punto.Id, punto.Nombre, _api);
        await Shell.Current.Navigation.PushModalAsync(popup);
        var vmPopup = popup.BindingContext as IncidenciaModalViewModel;
        if (vmPopup != null && vmPopup.Aceptado)
        {
            await GuardarRespuestaAsync(punto.Id, false, vmPopup.Comentario);
            await CrearIncidenciaAsync(punto.Id, vmPopup.Comentario, vmPopup.FotoBase64, vmPopup.MimeType);
        }
    }

    [RelayCommand]
    private async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}

// Clases auxiliares movidas fuera de RondinViewModel, en el mismo namespace
public partial class CategoriaChecklistVm : ObservableObject
{
    public string Nombre { get; set; } = string.Empty;
    public ObservableCollection<PuntoChecklistVm> Puntos { get; set; } = new();
}

public partial class PuntoChecklistVm : ObservableObject
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }

    [ObservableProperty] private bool? respuesta;
    [ObservableProperty] private string? comentario;
    [ObservableProperty] private string? fotoBase64;
    [ObservableProperty] private string? mimeType;
    [ObservableProperty] private string textoFoto = "📷 Tomar foto";
    [ObservableProperty] private bool panelNoVisible = false;

    public Color SiBackground => Respuesta == true ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F4F8F4");
    public Color SiBorder => Respuesta == true ? Color.FromArgb("#4CAF50") : Color.FromArgb("#C8DFC8");
    public Color SiTextColor => Respuesta == true ? Colors.White : Color.FromArgb("#1B3A1B");
    public Color NoBackground => Respuesta == false ? Color.FromArgb("#F44336") : Color.FromArgb("#F4F8F4");
    public Color NoBorder => Respuesta == false ? Color.FromArgb("#F44336") : Color.FromArgb("#C8DFC8");
    public Color NoTextColor => Respuesta == false ? Colors.White : Color.FromArgb("#1B3A1B");
    public string BorderColor => Respuesta switch
    {
        true => "#4CAF50",
        false => "#F44336",
        _ => "#E0EFE0"
    };
    public bool TieneDescripcion => !string.IsNullOrWhiteSpace(Descripcion);

    public void ActualizarApariencia(bool? nuevaRespuesta)
    {
        Respuesta = nuevaRespuesta;
        OnPropertyChanged(nameof(SiBackground));
        OnPropertyChanged(nameof(SiBorder));
        OnPropertyChanged(nameof(SiTextColor));
        OnPropertyChanged(nameof(NoBackground));
        OnPropertyChanged(nameof(NoBorder));
        OnPropertyChanged(nameof(NoTextColor));
        OnPropertyChanged(nameof(BorderColor));
        PanelNoVisible = nuevaRespuesta == false;
    }
}