using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RoclandGuardiaRelevo.Mobile.Models;

namespace RoclandGuardiaRelevo.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly IServiceProvider _serviceProvider;

    private static string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? AppConstants.BaseUrlAndroid : AppConstants.BaseUrlWindows;

    private const string ApiBasePath = "api/mob/guardiarelevo";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var handler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            }
        };
        _http = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };
    }

    private void SetAuthHeader()
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !string.IsNullOrEmpty(authService.Token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);
    }

    public void SetAuthToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // ======================== AUTENTICACIÓN ========================
    public async Task<LoginResponse?> LoginDirectoAsync(string username, string password)
    {
        try
        {
            var payload = new
            {
                Username = username,
                Password = password,
                CodigoProyecto = AppConstants.CodigoProyectoGuardiaRelevo,
                Plataforma = AppConstants.PlataformaMobile
            };
            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/login-directo", payload);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        }
        catch { return null; }
    }

    public async Task<LoginResponse?> LoginQrAsync(string qrCode)
    {
        try
        {
            var payload = new
            {
                QrCode = qrCode,
                CodigoProyecto = AppConstants.CodigoProyectoGuardiaRelevo,
                Plataforma = AppConstants.PlataformaMobile
            };
            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/login-qr", payload);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        }
        catch { return null; }
    }

    public async Task<MiPerfilResponse?> ObtenerMiPerfilAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Auth/mi-perfil");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<MiPerfilResponse>(JsonOpts);
        }
        catch { return null; }
    }

    public async Task<LoginResponse?> RefrescarTokenAsync(string refreshToken)
    {
        try
        {
            var payload = new { RefreshToken = refreshToken };
            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/refresh", payload);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        }
        catch { return null; }
    }

    // ======================== PUNTOS ACTIVOS ========================
    public async Task<List<PuntoDto>?> GetPuntosActivosAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/puntos/activos");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<PuntoDto>>(JsonOpts);
        }
        catch { return null; }
    }

    // ======================== CHECKLIST (RONDINES) ========================
    public async Task<GuardarChecklistResponseDto?> GuardarChecklistAsync(GuardarChecklistDto dto)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/checklist", dto);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<GuardarChecklistResponseDto>(JsonOpts);
        }
        catch { return null; }
    }

    public async Task<List<ChecklistResumenDto>?> GetHistorialAsync(int? idGuardia = null, DateTime? desde = null, DateTime? hasta = null)
    {
        SetAuthHeader();

        // Quitamos las fechas de la URL para evitar el bug de conversión en el servidor
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (idGuardia.HasValue) query["idGuardia"] = idGuardia.Value.ToString();
        var url = $"{ApiBasePath}/checklist/historial?{query}";

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var list = await response.Content.ReadFromJsonAsync<List<ChecklistResumenDto>>(JsonOpts);
            if (list == null) return null;

            // Filtramos de forma 100% segura usando la zona horaria del celular
            if (desde.HasValue)
                list = list.Where(x => x.FechaHoraLocal.Date >= desde.Value.Date).ToList();
            if (hasta.HasValue)
                list = list.Where(x => x.FechaHoraLocal.Date <= hasta.Value.Date).ToList();

            return list;
        }
        catch { return null; }
    }

    public async Task<ChecklistDetalleDto?> GetDetalleChecklistAsync(int idChecklist)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/checklist/{idChecklist}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ChecklistDetalleDto>(JsonOpts);
        }
        catch { return null; }
    }

    // ======================== INCIDENCIAS ========================
    public async Task<List<IncidenciaDto>?> GetIncidenciasAsync(bool? resuelta = false)
    {
        SetAuthHeader();
        var query = resuelta.HasValue ? $"?resuelta={resuelta.Value}" : "";
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/incidencias{query}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<IncidenciaDto>>(JsonOpts);
        }
        catch { return null; }
    }

    public async Task<bool> ResolverIncidenciaAsync(int idIncidencia)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsync($"{ApiBasePath}/incidencias/{idIncidencia}/resolver", null);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ======================== FOTOS ========================
    public async Task<int?> AgregarFotoAsync(AgregarFotoDto dto)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/fotos", dto);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            return result?.GetProperty("idFoto").GetInt32();
        }
        catch { return null; }
    }

    public async Task<byte[]?> GetFotoAsync(int idFoto)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/fotos/{idFoto}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch { return null; }
    }
}