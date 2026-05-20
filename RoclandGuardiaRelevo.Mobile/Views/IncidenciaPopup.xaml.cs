using RoclandGuardiaRelevo.Mobile.Services;
using RoclandGuardiaRelevo.Mobile.ViewModels;

namespace RoclandGuardiaRelevo.Mobile.Views;

public partial class IncidenciaPopup : ContentPage
{
    public IncidenciaPopup(int puntoId, string puntoNombre, ApiService api)
    {
        InitializeComponent();
        var vm = new IncidenciaModalViewModel(api)
        {
            PuntoId = puntoId,
            PuntoNombre = puntoNombre
        };
        BindingContext = vm;
    }
}