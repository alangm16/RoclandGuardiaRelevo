using RoclandGuardiaRelevo.Mobile.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    private void SetAuthHeader()
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !string.IsNullOrEmpty(authService.Token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);
        }
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
            var rawJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlertAsync("Error Backend", $"Status: {response.StatusCode}\n{rawJson}", "OK");
                });
                return null;
            }

            return JsonSerializer.Deserialize<LoginResponse>(rawJson, JsonOpts);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlertAsync("Error al convertir JSON", ex.Message, "OK");
            });
            return null;
        }
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
        catch (Exception ex)
        {
            Console.WriteLine($"LoginQrAsync error: {ex.Message}");
            return null;
        }
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
        catch
        {
            return null;
        }
    }

    // ======================== RELEVO Y RONDÍN ========================

    /// <summary>
    /// Obtiene o crea el relevo activo del guardia autenticado, junto con el participante asociado.
    /// </summary>
    public async Task<MiActivoResponse?> GetMiRelevoActivoAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Relevo/mi-activo");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<MiActivoResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtiene el detalle completo de un relevo.
    /// </summary>
    public async Task<RelevoDetalleResponse?> GetRelevoDetalleAsync(int relevoId)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Relevo/{relevoId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<RelevoDetalleResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtiene el historial paginado de relevos.
    /// </summary>
    public async Task<PagedResult<RelevoListResponse>?> GetHistorialRelevosAsync(int page = 1, int pageSize = 10)
    {
        SetAuthHeader();
        try
        {
            var url = $"{ApiBasePath}/Relevo/paged?page={page}&pageSize={pageSize}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<PagedResult<RelevoListResponse>>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    // ======================== RONDÍN (iniciar, guardar, completar) ========================

    /// <summary>
    /// Inicia el rondín automáticamente para el guardia autenticado.
    /// </summary>
    public async Task<IniciarRondinResponse?> IniciarRondinAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsync($"{ApiBasePath}/Rondin/iniciar", null);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IniciarRondinResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Guarda la respuesta de un punto del checklist.
    /// </summary>
    public async Task<GuardarRespuestaResponse?> GuardarRespuestaAsync(int participanteId, GuardarRespuestaRequest request)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/Rondin/{participanteId}/respuesta", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<GuardarRespuestaResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Completa el rondín, guarda firma y observaciones.
    /// </summary>
    public async Task<CompletarRondinResponse?> CompletarRondinAsync(int participanteId, CompletarRondinRequest request)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/Rondin/{participanteId}/completar", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CompletarRondinResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    // ======================== CHECKLIST ========================

    public async Task<List<CategoriaChecklistDto>?> GetChecklistPuntosAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Checklist/puntos");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<CategoriaChecklistDto>>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ChecklistRespuestaResponse>?> GetRespuestasPorParticipanteAsync(int participanteId)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Checklist/respuestas/{participanteId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<ChecklistRespuestaResponse>>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<DiscrepanciaRespuestaResponse>?> GetDiscrepanciasAsync(int relevoId)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Checklist/discrepancias/{relevoId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<DiscrepanciaRespuestaResponse>>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    // ======================== INCIDENCIAS ========================

    public async Task<IncidenciaResponse?> CrearIncidenciaAsync(CrearIncidenciaRequest request)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/Incidencia", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IncidenciaResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<IncidenciaResponse>?> GetIncidenciasPorRelevoAsync(int relevoId)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Incidencia/relevo/{relevoId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<IncidenciaResponse>>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IncidenciaResponse?> GetIncidenciaDetalleAsync(int incidenciaId)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/Incidencia/{incidenciaId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IncidenciaResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IncidenciaResponse?> ActualizarIncidenciaAsync(int incidenciaId, ActualizarIncidenciaRequest request)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PutAsJsonAsync($"{ApiBasePath}/Incidencia/{incidenciaId}", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IncidenciaResponse>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    // ======================== CONFIGURACIÓN ========================

    public async Task<List<ConfigTurnoResponse>?> GetConfigTurnosActivosAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync($"{ApiBasePath}/ConfigTurno/activos");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<ConfigTurnoResponse>>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}