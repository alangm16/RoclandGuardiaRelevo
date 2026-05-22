using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Services;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class IncidenciaModalViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private int puntoId;
    [ObservableProperty] private string puntoNombre = string.Empty;
    [ObservableProperty] private string comentario = string.Empty;
    [ObservableProperty] private string? fotoBase64;
    [ObservableProperty] private string? mimeType;
    [ObservableProperty] private bool tieneFoto;
    [ObservableProperty] private bool tieneNoFoto = true;   
    [ObservableProperty] private ImageSource? fotoPreview;  
    [ObservableProperty] private bool estaCargando;

    public bool Aceptado { get; private set; }

    public IncidenciaModalViewModel(ApiService api, int puntoId, string puntoNombre)
    {
        _api = api;
        PuntoId = puntoId;
        PuntoNombre = puntoNombre;
    }

    [RelayCommand]
    private async Task TomarFotoAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return;

            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            FotoBase64 = Convert.ToBase64String(bytes);
            MimeType = photo.ContentType;
            TieneFoto = true;
            TieneNoFoto = false;                                       
            FotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Error", "No se pudo tomar la foto", "OK");
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        Aceptado = false;
        Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Comentario))
        {
            await Shell.Current.DisplayAlertAsync("Campo requerido",
                "Debes escribir un comentario para reportar la anomalía.", "OK");
            return;
        }

        Aceptado = true;
        await Shell.Current.Navigation.PopModalAsync();
    }
}