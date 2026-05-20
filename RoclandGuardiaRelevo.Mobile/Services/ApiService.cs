using RoclandGuardiaRelevo.Mobile.Models;
using System.Net;
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

    // Ruta base unificada para todos los endpoints móviles de Guardia Relevo
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
            if (!response.IsSuccessStatusCode) return null;
            var rawJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(rawJson, JsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoginDirectoAsync error: {ex.Message}");
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

    // ======================== RELEVO Y PARTICIPANTE ========================

    /// <summary>
    /// Obtiene o crea el relevo activo del guardia autenticado, junto con el participante asociado.
    /// Endpoint principal para la pantalla de inicio.
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
    /// Obtiene el detalle completo de un relevo (participantes, respuestas, incidencias).
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
    public async Task<PagedResult<HistorialRelevoItem>?> GetHistorialRelevosAsync(int page = 1, int pageSize = 10)
    {
        SetAuthHeader();
        try
        {
            var url = $"{ApiBasePath}/Relevo?page={page}&pageSize={pageSize}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<PagedResult<HistorialRelevoItem>>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Inicia el turno del participante (cambia estado a EnCurso).
    /// </summary>
    public async Task<bool> IniciarParticipanteAsync(int participanteId, string? observaciones = null)
    {
        SetAuthHeader();
        try
        {
            var payload = new IniciarParticipanteRequest { Observaciones = observaciones };
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/Participante/{participanteId}/iniciar", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Cierra el turno del participante: envía todas las respuestas y la firma.
    /// </summary>
    public async Task<bool> CerrarParticipanteAsync(int participanteId, CerrarParticipanteRequest request)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/Participante/{participanteId}/cerrar", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ======================== CHECKLIST ========================

    /// <summary>
    /// Obtiene todos los puntos del checklist agrupados por categoría.
    /// </summary>
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

    /// <summary>
    /// Guarda o actualiza una respuesta individual del checklist.
    /// </summary>
    public async Task<bool> GuardarRespuestaAsync(int participanteId, GuardarRespuestaRequest request)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/Checklist/respuestas/{participanteId}", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtiene todas las respuestas ya guardadas de un participante (útil para restaurar estado).
    /// </summary>
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

    // ======================== INCIDENCIAS ========================

    /// <summary>
    /// Crea una nueva incidencia (por NoOk o Discrepancia) con foto opcional.
    /// </summary>
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

    /// <summary>
    /// Obtiene todas las incidencias de un relevo.
    /// </summary>
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

    /// <summary>
    /// Obtiene el detalle de una incidencia específica.
    /// </summary>
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

    /// <summary>
    /// Actualiza descripción o foto de una incidencia abierta.
    /// </summary>
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

    // ======================== CONFIGURACIÓN (OPCIONAL) ========================

    /// <summary>
    /// Obtiene la lista de configuraciones de turno activas (solo para administración).
    /// </summary>
    public async Task<List<ConfigTurnoResponse>?> GetConfigTurnosAsync()
    {
        SetAuthHeader();
        try
        {
            // Nota: Este endpoint aún no existe en los controladores; crearlo si se necesita.
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