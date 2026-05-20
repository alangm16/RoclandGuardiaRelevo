using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandGuardiaRelevo.Mobile.Services;

namespace RoclandGuardiaRelevo.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    [ObservableProperty] private string _usuario = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _hayError;

    public LoginViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        Titulo = "Guardia - Acceso";
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        if(string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            MostrarError("Ingresa usuario y contraseña.");
            return;
        }

        EstaCargando = true;
        HayError = false;

        try
        {
            var loginResponse = await _api.LoginDirectoAsync(Usuario, Password);

            if(loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                MostrarError("Credenciales inválidas.");
                return;
            }

            _api.SetAuthToken(loginResponse.Token);

            var perfil = await _api.ObtenerMiPerfilAsync();

            if (perfil is null)
            {
                MostrarError("No tienes un perfil asignado en Guardia Relevo.");
                return;
            }

            _auth.GuardarSesion(loginResponse.Token, perfil.NombreCompleto, perfil.PerfilId);

            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            MostrarError("Error al iniciar sesión. Intenta nuevamente.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    public async Task IniciarSesionQrAsync(string qr)
    {
        if (string.IsNullOrWhiteSpace(qr))
        {
            MostrarError("Código QR inválido.");
            return;
        }

        EstaCargando = true;
        HayError = false;

        try
        {
            var loginResponse = await _api.LoginQrAsync(qr);

            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                MostrarError("Código QR inválido o expirado.");
                return;
            }

            _api.SetAuthToken(loginResponse.Token);

            var perfil = await _api.ObtenerMiPerfilAsync();

            if (perfil is null)
            {
                MostrarError("No tienes un perfil asignado en Guardia Relevo.");
                return;
            }

            _auth.GuardarSesion(loginResponse.Token, perfil.NombreCompleto, perfil.PerfilId);

            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            MostrarError("Error al iniciar sesión con QR. Intenta nuevamente.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void MostrarError(string msg)
    {
        MensajeError = msg;
        HayError = true;
    }
}