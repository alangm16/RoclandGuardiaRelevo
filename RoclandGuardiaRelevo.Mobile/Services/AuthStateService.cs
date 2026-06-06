using RoclandGuardiaRelevo.Mobile.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoclandGuardiaRelevo.Mobile.Services;

public class AuthStateService
{
    private readonly ApiService _api;
    private const string TokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private const string UserIdKey = "user_id";
    private const string UserNameKey = "user_name";
    private const string FullNameKey = "full_name";
    private const string TurnoKey = "turno";

    public string? Token { get; private set; }
    public string? RefreshToken { get; private set; }
    public int IdGuardia { get; private set; }
    public string NombreGuardia { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Turno { get; private set; } = string.Empty; // "Diurno" o "Nocturno"

    public AuthStateService(ApiService api)
    {
        _api = api;
    }

    public async Task<bool> IniciarSesionAsync(string username, string password)
    {
        var loginResult = await _api.LoginDirectoAsync(username, password);
        if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
            return false;

        await GuardarSesion(loginResult);
        await CargarPerfilYTurno();
        return true;
    }

    public async Task<bool> IniciarSesionPorQrAsync(string qrCode)
    {
        var loginResult = await _api.LoginQrAsync(qrCode);
        if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
            return false;

        await GuardarSesion(loginResult);
        await CargarPerfilYTurno();
        return true;
    }

    private async Task GuardarSesion(LoginResponse loginResult)
    {
        Token = loginResult.Token;
        RefreshToken = loginResult.RefreshToken;
        _api.SetAuthToken(Token);

        Preferences.Set(TokenKey, Token);
        Preferences.Set(RefreshTokenKey, RefreshToken);
        if (loginResult.Usuario != null)
        {
            IdGuardia = loginResult.Usuario.Id;
            Username = loginResult.Usuario.Username;
            NombreGuardia = loginResult.Usuario.NombreCompleto;  // ← ya está aquí

            Preferences.Set(UserIdKey, IdGuardia);
            Preferences.Set(UserNameKey, Username);
            Preferences.Set(FullNameKey, NombreGuardia);
        }
    }

    private async Task CargarPerfilYTurno()
    {
        var perfil = await _api.ObtenerMiPerfilAsync();
        if (perfil != null)
        {
            // Solo actualizar si vienen datos, pero no son críticos
            if (!string.IsNullOrEmpty(perfil.NombreCompleto))
                NombreGuardia = perfil.NombreCompleto;
            if (!string.IsNullOrEmpty(perfil.Username))
                Username = perfil.Username;
            // El turno no viene del backend, lo calculamos nosotros
        }

        // Calcular turno basado en usuario/nombre
        Turno = DeterminarTurnoPorCredenciales(Username, NombreGuardia);

        // Guardar en Preferences
        Preferences.Set(TurnoKey, Turno);
        Preferences.Set(FullNameKey, NombreGuardia);
    }

    public async Task<bool> RestaurarSesionAsync()
    {
        return await CargarSesionGuardada();
    }

    public async Task<bool> GarantizarTokenValidoAsync()
    {
        // Si el token expira pronto (ej. en menos de 5 min), refrescar
        if (Token != null && !string.IsNullOrEmpty(RefreshToken))
        {
            // Opcional: decodificar JWT y verificar expiración.
            // Por simplicidad, se puede refrescar siempre o confiar en el backend.
            return await RefrescarTokenAsync();
        }
        return Token != null;
    }

    /// <summary>
    /// Determina el turno del guardia basado en su username o nombre completo.
    /// Valores fijos:
    ///   - jerivez / José Manuel Erivez López → Diurno
    ///   - jsantos / Jesús Santos Córdoba Hidrogo → Nocturno
    /// </summary>
    private string DeterminarTurnoPorCredenciales(string username, string nombreCompleto)
    {
        // Normalizar para comparación (minúsculas, sin acentos opcional)
        var userLower = username?.ToLowerInvariant() ?? "";
        var nameLower = nombreCompleto?.ToLowerInvariant() ?? "";

        // Detectar por usuario (más seguro)
        if (userLower == "jerivez")
            return "Diurno";
        if (userLower == "jsantos")
            return "Nocturno";

        // Fallback: detectar por partes del nombre
        if (nameLower.Contains("erivez"))
            return "Diurno";
        if (nameLower.Contains("santos"))
            return "Nocturno";

        // Valor por defecto (diurno, pero se puede cambiar según necesidades)
        return "Diurno";
    }

    public async Task<bool> RefrescarTokenAsync()
    {
        if (string.IsNullOrEmpty(RefreshToken)) return false;
        var newTokens = await _api.RefrescarTokenAsync(RefreshToken);
        if (newTokens == null) return false;
        Token = newTokens.Token;
        RefreshToken = newTokens.RefreshToken;
        _api.SetAuthToken(Token);
        Preferences.Set(TokenKey, Token);
        Preferences.Set(RefreshTokenKey, RefreshToken);
        return true;
    }

    public void CerrarSesion()
    {
        Preferences.Clear();
        Token = null;
        RefreshToken = null;
        IdGuardia = 0;
        NombreGuardia = string.Empty;
        Username = string.Empty;
        Turno = string.Empty;
        _api.SetAuthToken(string.Empty);
    }

    public async Task<bool> CargarSesionGuardada()
    {
        var token = Preferences.Get(TokenKey, string.Empty);
        if (string.IsNullOrEmpty(token)) return false;
        Token = token;
        RefreshToken = Preferences.Get(RefreshTokenKey, string.Empty);
        IdGuardia = Preferences.Get(UserIdKey, 0);
        NombreGuardia = Preferences.Get(FullNameKey, string.Empty);
        Username = Preferences.Get(UserNameKey, string.Empty);
        Turno = Preferences.Get(TurnoKey, string.Empty);

        if (string.IsNullOrEmpty(Turno))
            Turno = DeterminarTurnoPorCredenciales(Username, NombreGuardia);

        _api.SetAuthToken(Token);
        return true;
    }
}