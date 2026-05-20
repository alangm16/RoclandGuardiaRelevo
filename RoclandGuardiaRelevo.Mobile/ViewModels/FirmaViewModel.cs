using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class FirmaViewModel : ObservableObject
{
    public List<SKPath> Paths { get; } = new();

    public Action? RequestRedraw { get; set; }

    [ObservableProperty]
    private byte[]? firmaBytes;

    [ObservableProperty]
    private bool firmaCompletada;

    public void Draw(SKCanvas canvas)
    {
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 3,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        foreach (var path in Paths)
            canvas.DrawPath(path, paint);
    }

    [RelayCommand]
    private void Limpiar()
    {
        Paths.Clear();
        RequestRedraw?.Invoke();
    }

    [RelayCommand]
    private async Task Aceptar()
    {
        if (Paths.Count == 0)
            return;

        FirmaBytes = ExportarFirma();
        FirmaCompletada = true;

        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task Cancelar()
    {
        FirmaCompletada = false;
        await Shell.Current.Navigation.PopModalAsync();
    }

    private byte[] ExportarFirma(int width = 400, int height = 200)
    {
        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        Draw(canvas);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }
}