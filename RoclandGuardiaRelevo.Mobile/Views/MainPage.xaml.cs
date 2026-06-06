using RoclandGuardiaRelevo.Mobile.ViewModels;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MainViewModel mainVm)
            await mainVm.CargarDatosAsync();
    }
}