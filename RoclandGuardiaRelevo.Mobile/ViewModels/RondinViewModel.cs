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
    private string tipoRondin = string.Empty;
    private List<PuntoConRespuesta> todosLosPuntos = new();

    [ObservableProperty] private string subtitulo = string.Empty;
    [ObservableProperty] private string instruccionTexto = string.Empty;
    [ObservableProperty] private string progresoTexto = "0/0";
    [ObservableProperty] private double progresoValor = 0;
    [ObservableProperty] private string observacionGeneral = string.Empty;
    [ObservableProperty] private bool puedeEnviar;
    [ObservableProperty] private Color colorBoton = Colors.Gray;
    [ObservableProperty] private ObservableCollection<CategoriaChecklistVm> categorias = new();
    [ObservableProperty] private ObservableCollection<FotoPreviewItem> fotosPreview = new();

    public RondinViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
    }

    public async Task InicializarAsync(string tipo)
    {
        tipoRondin = tipo;

        // 1. VERIFICACIÓN DE DUPLICADOS (Depende del modo)
        if (AppConstants.ModoPruebas)
        {
            // MODO PRUEBAS: Solo confiamos en el flag local para bloquear.
            if (RondinFlagsService.EstaEnviado(_auth.IdGuardia, tipoRondin))
            {
                await Shell.Current.DisplayAlertAsync("Ya completado",
                    $"El rondín {AppConstants.DescripcionTipoRondin(tipo)} ya se envió en esta sesión de prueba.", "OK");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }
        }
        else
        {
            // MODO PRODUCCIÓN: Consultamos la BD real para evitar duplicidad global
            var hoy = DateTime.Today;
            var historialHoy = await _api.GetHistorialAsync(idGuardia: null, desde: hoy, hasta: hoy);
            if (historialHoy?.Any(h => h.TipoRondin == tipo) == true)
            {
                await Shell.Current.DisplayAlertAsync("Ya completado",
                    $"El rondín {AppConstants.DescripcionTipoRondin(tipo)} ya se encuentra registrado hoy en el sistema.", "OK");
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }
        }

        Subtitulo = tipo switch
        {
            "AMS" or "AVS" => "Entrega de turno (Saliente)",
            "BME" or "BVE" => "Recepción de turno (Entrante)",
            _ => "Rondín"
        };
        InstruccionTexto = "Marca SÍ (correcto) o NO (problema) en cada punto. Al finalizar puedes agregar observaciones y fotos.";

        await CargarPuntosAsync();
    }

    private async Task CargarPuntosAsync()
    {
        EstaCargando = true;
        try
        {
            var puntos = await _api.GetPuntosActivosAsync();
            if (puntos == null || !puntos.Any())
            {
                await Shell.Current.DisplayAlertAsync("Error", "No hay puntos de checklist configurados.", "OK");
                return;
            }

            var grupos = puntos.OrderBy(p => p.Orden).GroupBy(p => p.Categoria);
            var nuevasCategorias = new ObservableCollection<CategoriaChecklistVm>();
            todosLosPuntos.Clear();

            foreach (var grupo in grupos)
            {
                var catVm = new CategoriaChecklistVm { Nombre = grupo.Key };
                foreach (var p in grupo)
                {
                    var puntoVm = new PuntoConRespuesta
                    {
                        Id = p.Id,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion,
                        Orden = p.Orden
                    };
                    puntoVm.PropertyChanged += (s, e) => ActualizarProgreso();
                    catVm.Puntos.Add(puntoVm);
                    todosLosPuntos.Add(puntoVm);
                }
                nuevasCategorias.Add(catVm);
            }
            Categorias = nuevasCategorias;
            ActualizarProgreso();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"No se pudo cargar el checklist: {ex.Message}", "OK");
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
        PuedeEnviar = respondidos == total && total > 0;
        ColorBoton = PuedeEnviar ? Color.FromArgb("#2E7D32") : Color.FromArgb("#9E9E9E");
    }

    [RelayCommand]
    private void ResponderSi(PuntoConRespuesta punto)
        => punto.Respuesta = true;

    [RelayCommand]
    private void ResponderNo(PuntoConRespuesta punto)
        => punto.Respuesta = false;

    // Llamado desde el code-behind tras capturar foto
    public async Task AgregarFotoAsync(byte[] fotoBytes, string mimeType)
    {
        var imageSource = ImageSource.FromStream(() => new MemoryStream(fotoBytes));
        FotosPreview.Add(new FotoPreviewItem
        {
            Thumbnail = imageSource,
            Bytes = fotoBytes,
            MimeType = mimeType
        });
    }

    [RelayCommand]
    private void EliminarFoto(FotoPreviewItem foto)
        => FotosPreview.Remove(foto);

    [RelayCommand]
    private async Task EnviarRondinAsync()
    {
        if (!PuedeEnviar)
        {
            await Shell.Current.DisplayAlertAsync("Incompleto", "Responde todos los puntos antes de finalizar.", "OK");
            return;
        }

        // Última verificación en el servidor justo antes de iniciar el guardado
        var hoy = DateTime.Today;
        var historialHoy = await _api.GetHistorialAsync(idGuardia: null, desde: hoy, hasta: hoy);
        if (historialHoy?.Any(h => h.TipoRondin == tipoRondin) == true)
        {
            await Shell.Current.DisplayAlertAsync("Ya completado",
                "El otro guardia ya envió este rondín hace un momento.", "OK");
            await Shell.Current.GoToAsync("//MainPage");
            return;
        }

        // Pedir firma
        var firmaVm = new FirmaViewModel();
        var firmaPage = new FirmaPage(firmaVm);
        var tcsFirma = new TaskCompletionSource<bool>();
        firmaPage.Disappearing += (s, e) => tcsFirma.TrySetResult(true);
        await Shell.Current.Navigation.PushModalAsync(firmaPage);
        await tcsFirma.Task;

        if (!firmaVm.FirmaCompletada || firmaVm.FirmaBytes == null)
            return;

        var puntosDto = todosLosPuntos.Select(p => new ChecklistPuntoItemDto
        {
            IdPunto = p.Id,
            Estado = p.Respuesta!.Value
        }).ToList();

        var guardarDto = new GuardarChecklistDto
        {
            TipoRondin = tipoRondin,
            Observacion = ObservacionGeneral,
            Firma = firmaVm.FirmaBytes,
            Puntos = puntosDto
        };

        EstaCargando = true;
        try
        {
            var resultado = await _api.GuardarChecklistAsync(guardarDto);
            if (resultado == null || resultado.IdChecklist == 0)
                throw new Exception("El servidor no devolvió un Id válido.");

            int idChecklist = resultado.IdChecklist;

            foreach (var foto in FotosPreview)
            {
                await _api.AgregarFotoAsync(new AgregarFotoDto
                {
                    IdChecklist = idChecklist,
                    Foto = foto.Bytes,
                    MimeType = foto.MimeType
                });
            }

            RondinFlagsService.RegistrarGuardiaActivo(_auth.IdGuardia);
            RondinFlagsService.MarcarEnviado(_auth.IdGuardia, tipoRondin);

            string msg = $"Rondín guardado correctamente.\nIncidencias generadas: {resultado.IncidenciasGeneradas}";
            await Shell.Current.DisplayAlertAsync("Éxito", msg, "OK");
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task VolverAsync()
        => await Shell.Current.GoToAsync("//MainPage");
}

// ── Clases auxiliares ──────────────────────────────────────────────────

public partial class CategoriaChecklistVm : ObservableObject
{
    public string Nombre { get; set; } = string.Empty;
    public ObservableCollection<PuntoConRespuesta> Puntos { get; set; } = new();
}

public partial class PuntoConRespuesta : ObservableObject
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; }

    public bool TieneDescripcion => !string.IsNullOrWhiteSpace(Descripcion);

    [ObservableProperty]
    private bool? respuesta;

    // Notifica los colores y textos cada vez que cambia la respuesta
    partial void OnRespuestaChanged(bool? value)
    {
        OnPropertyChanged(nameof(SiBackground));
        OnPropertyChanged(nameof(NoBackground));
        OnPropertyChanged(nameof(SiTextColor));
        OnPropertyChanged(nameof(NoTextColor));
    }

    // Color neutro inicial: #D8E8D8 (gris-verde suave igual para ambos)
    // Seleccionado SÍ → verde #2E7D32  |  Seleccionado NO → rojo #C62828
    public Color SiBackground => Respuesta == true ? Color.FromArgb("#2E7D32") : Color.FromArgb("#D8E8D8");
    public Color NoBackground => Respuesta == false ? Color.FromArgb("#C62828") : Color.FromArgb("#D8E8D8");

    public Color SiTextColor => Respuesta == true ? Colors.White : Color.FromArgb("#3A5A3A");
    public Color NoTextColor => Respuesta == false ? Colors.White : Color.FromArgb("#5A3A3A");
}

public class FotoPreviewItem
{
    public ImageSource Thumbnail { get; set; } = null!;
    public byte[] Bytes { get; set; } = null!;
    public string MimeType { get; set; } = string.Empty;
}