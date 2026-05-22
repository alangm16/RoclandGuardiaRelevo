using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class FirmaViewModel : ObservableObject
{
    public List<SKPath> Paths { get; } = new();
    public Action? RequestRedraw { get; set; }
    public SKSize CanvasSize { get; set; } = new SKSize(800, 400);

    [ObservableProperty] private byte[]? firmaBytes;
    [ObservableProperty] private bool firmaCompletada;

    public void Draw(SKCanvas canvas)
    {
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 3f,
            StrokeCap = SKStrokeCap.Round,   
            StrokeJoin = SKStrokeJoin.Round,
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
        if (Paths.Count == 0) return;

        FirmaBytes = ExportarFirma((int)CanvasSize.Width, (int)CanvasSize.Height);
        FirmaCompletada = true;
        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task Cancelar()
    {
        FirmaCompletada = false;
        await Shell.Current.Navigation.PopModalAsync();
    }

    private byte[] ExportarFirma(int width, int height)
    {
        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        Draw(surface.Canvas);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}