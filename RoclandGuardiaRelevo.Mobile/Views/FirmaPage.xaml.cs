using RoclandGuardiaRelevo.Mobile.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class FirmaPage : ContentPage
{
    private readonly FirmaViewModel _vm;
    private SKPath? _currentPath;
    private SKPoint _lastPoint;

    public FirmaPage(FirmaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _vm.RequestRedraw = () => FirmaCanvas.InvalidateSurface();
    }

    private void OnCanvasTouch(object sender, SKTouchEventArgs e)
    {
        var pt = e.Location;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _currentPath = new SKPath();
                _currentPath.MoveTo(pt);
                _lastPoint = pt;
                _vm.Paths.Add(_currentPath);

                // Ocultar hint en el primer trazo
                if (HintLabel.IsVisible)
                    HintLabel.IsVisible = false;
                break;

            case SKTouchAction.Moved:
                if (_currentPath != null)
                {
                    // Curva suave con punto de control = promedio entre último y actual
                    var mid = new SKPoint((_lastPoint.X + pt.X) / 2f, (_lastPoint.Y + pt.Y) / 2f);
                    _currentPath.QuadTo(_lastPoint, mid);
                    _lastPoint = pt;
                }
                break;

            case SKTouchAction.Released:
                // Cerrar el trazo suavemente al punto final
                _currentPath?.LineTo(pt);
                _currentPath = null;
                break;

            case SKTouchAction.Cancelled:
                _currentPath = null;
                break;
        }

        e.Handled = true;
        FirmaCanvas.InvalidateSurface();
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        _vm.CanvasSize = new SKSize(e.Info.Width, e.Info.Height); // ← tamaño real en píxeles
        _vm.Draw(e.Surface.Canvas);
    }
}