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
        if (!query.TryGetValue("participanteId", out var pIdObj) ||
            !int.TryParse(pIdObj?.ToString(), out var pId))
            return;

        if (!query.TryGetValue("rol", out var rolObj))
            return;

        if (!query.TryGetValue("relevoId", out var rIdObj) ||
            !int.TryParse(rIdObj?.ToString(), out var rId))
            return;

        await _vm.CargarChecklistAsync(pId, rolObj.ToString()!, rId);
    }
}