using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Models;
using RoclandGuardiaRelevo.Mobile.Services;
using System.Collections.ObjectModel;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class IncidenciasViewModel : BaseViewModel
{
    private readonly ApiService _api;

    [ObservableProperty] private ObservableCollection<IncidenciaDto> incidencias = new();
    [ObservableProperty] private bool mostrarSoloAbiertas = true;
    [ObservableProperty] private bool estaCargandoIncidencias;

    public IncidenciasViewModel(ApiService api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task CargarIncidenciasAsync()
    {
        if (EstaCargandoIncidencias) return;
        EstaCargandoIncidencias = true;
        try
        {
            var lista = await _api.GetIncidenciasAsync(MostrarSoloAbiertas);
            if (lista != null)
            {
                Incidencias.Clear();
                foreach (var inc in lista)
                    Incidencias.Add(inc);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            EstaCargandoIncidencias = false;
        }
    }

    [RelayCommand]
    private async Task AlternarFiltroAsync()
    {
        MostrarSoloAbiertas = !MostrarSoloAbiertas;
        await CargarIncidenciasAsync();
    }

    [RelayCommand]
    private async Task ResolverIncidenciaAsync(IncidenciaDto incidencia)
    {
        if (incidencia.Resuelta)
        {
            await Shell.Current.DisplayAlertAsync("Info", "Esta incidencia ya está resuelta.", "OK");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlertAsync("Resolver", $"¿Marcar como resuelta la incidencia del punto '{incidencia.Punto}'?", "Sí", "No");
        if (!confirm) return;

        var exito = await _api.ResolverIncidenciaAsync(incidencia.Id);
        if (exito)
        {
            await CargarIncidenciasAsync();
            await Shell.Current.DisplayAlertAsync("Éxito", "Incidencia resuelta.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "No se pudo resolver.", "OK");
        }
    }
}