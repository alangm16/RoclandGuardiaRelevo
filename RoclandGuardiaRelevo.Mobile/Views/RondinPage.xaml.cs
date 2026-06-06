using RoclandGuardiaRelevo.Mobile.Services;
using RoclandGuardiaRelevo.Mobile.ViewModels;
using Microsoft.Maui.Controls;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class RondinPage : ContentPage, IQueryAttributable
{
    private readonly RondinViewModel _vm;

    public RondinPage(RondinViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("tipoRondin", out var tipoObj) && tipoObj?.ToString() is string tipoRondin)
        {
            await _vm.InicializarAsync(tipoRondin);
        }
    }

    // Método para manejar la captura de fotos desde la cámara
    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return;

            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            await _vm.AgregarFotoAsync(bytes, photo.ContentType);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo tomar la foto: {ex.Message}", "OK");
        }
    }
}