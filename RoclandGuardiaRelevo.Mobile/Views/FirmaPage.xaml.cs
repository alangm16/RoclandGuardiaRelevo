using RoclandGuardiaRelevo.Mobile.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class FirmaPage : ContentPage
{
    private readonly FirmaViewModel _vm;
    private SKPath? _currentPath;

    public FirmaPage(FirmaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _vm.RequestRedraw = () => FirmaCanvas.InvalidateSurface();
    }

    private void OnCanvasTouch(object sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _currentPath = new SKPath();
                _currentPath.MoveTo(e.Location);
                _vm.Paths.Add(_currentPath);
                break;

            case SKTouchAction.Moved:
                _currentPath?.LineTo(e.Location);
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                _currentPath = null;
                break;
        }

        e.Handled = true;
        FirmaCanvas.InvalidateSurface();
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        _vm.Draw(e.Surface.Canvas);
    }
}