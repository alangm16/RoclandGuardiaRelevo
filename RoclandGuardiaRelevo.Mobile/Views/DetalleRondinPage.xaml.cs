using RoclandGuardiaRelevo.Mobile.ViewModels;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class DetalleRondinPage : ContentPage, IQueryAttributable
{
    private readonly DetalleRondinViewModel _vm;

    public DetalleRondinPage(DetalleRondinViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
        {
            await _vm.CargarDetalleAsync(id);
        }
    }
}