using CommunityToolkit.Mvvm.ComponentModel;
using RoclandGuardiaRelevo.Mobile.Models;
using RoclandGuardiaRelevo.Mobile.Services;
using System.Collections.ObjectModel;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class DetalleRondinViewModel : BaseViewModel
{
    private readonly ApiService _api;

    [ObservableProperty] private string tipoRondin = string.Empty;
    [ObservableProperty] private string descripcionRondin = string.Empty;
    [ObservableProperty] private string guardia = string.Empty;
    [ObservableProperty] private DateTime fechaHoraLocal;
    [ObservableProperty] private string? observacion;
    [ObservableProperty] private bool tieneFirma;
    [ObservableProperty] private ObservableCollection<ChecklistPuntoDetalleDto> puntos = new();

    public DetalleRondinViewModel(ApiService api)
    {
        _api = api;
    }

    public async Task CargarDetalleAsync(int idChecklist)
    {
        EstaCargando = true;
        try
        {
            var detalle = await _api.GetDetalleChecklistAsync(idChecklist);
            if (detalle == null)
                throw new Exception("No se encontró el rondín.");

            TipoRondin = detalle.TipoRondin;
            DescripcionRondin = detalle.DescripcionRondin;
            Guardia = detalle.Guardia;
            FechaHoraLocal = detalle.FechaHoraLocal;
            Observacion = detalle.Observacion;
            TieneFirma = detalle.TieneFirma;
            Puntos.Clear();
            foreach (var p in detalle.Puntos.OrderBy(p => p.Orden))
                Puntos.Add(p);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }
}