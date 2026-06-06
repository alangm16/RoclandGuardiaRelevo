using RoclandGuardiaRelevo.Mobile.ViewModels;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class IncidenciasPage : ContentPage
{
    public IncidenciasPage(IncidenciasViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IncidenciasViewModel incVm)
            incVm.CargarIncidenciasCommand.Execute(null);
    }
}