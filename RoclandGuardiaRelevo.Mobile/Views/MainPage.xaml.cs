using RoclandGuardiaRelevo.Mobile.ViewModels;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // --- AGREGAR ESTE MÉTODO ---
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Esto fuerza a la app a consultar la API cada vez que la pantalla principal aparece.
        // Así los indicadores grises pasarán a verde si el rondín ya se envió.
        if (BindingContext is MainViewModel vm)
        {
            vm.CargarDatosCommand.Execute(null);
        }
    }
    // ---------------------------
}