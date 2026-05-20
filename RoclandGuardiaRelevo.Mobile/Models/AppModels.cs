using System.Text.Json.Serialization;

namespace RoclandGuardiaRelevo.Mobile.Models;

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class QrLoginRequest
{
    [JsonPropertyName("qrCode")]
    public string QRCode { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string Token { get; set; } = string.Empty;
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("expiracion")]
    public DateTime Expiracion { get; set; }
    [JsonPropertyName("usuario")]
    public UsuarioTokenDto? Usuario { get; set; }

    // Propiedades de conveniencia (mapeadas desde Usuario)
    public string NombreCompleto => Usuario?.NombreCompleto ?? string.Empty;
    public string Username => Usuario?.Username ?? string.Empty;
    public int UsuarioId => Usuario?.Id ?? 0;
}

public class UsuarioTokenDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class MiPerfilResponse
{
    [JsonPropertyName("perfilId")]
    public int PerfilId { get; set; }
    [JsonPropertyName("superAdminUsuarioId")]
    public int SuperAdminUsuarioId { get; set; }
    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;
    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } = string.Empty;   // antes TipoPerfil
    [JsonPropertyName("nivelRol")]
    public int NivelRol { get; set; }
    [JsonPropertyName("turno")]
    public string? Turno { get; set; }
    [JsonPropertyName("numeroEmpleado")]
    public string? NumeroEmpleado { get; set; }
}

public class ConfigTurnoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public bool Activo { get; set; }
    public TimeOnly HoraInicioSaliente { get; set; }
    public TimeOnly HoraFinSaliente { get; set; }
    public TimeOnly HoraInicioEntrante { get; set; }
    public TimeOnly HoraFinEntrante { get; set; }
}

public class RelevoHoyResponse
{
    public int RelevoId { get; set; }
    public DateOnly Fecha { get; set; }
    public string NombreTurno { get; set; }
    public string Estado { get; set; }
    public TimeOnly HoraInicioSaliente { get; set; }
    public TimeOnly HoraFinSaliente { get; set; }
    public TimeOnly HoraInicioEntrante { get; set; }
    public TimeOnly HoraFinEntrante { get; set; }
    public ParticipanteSummaryResponse? Saliente { get; set; }
    public ParticipanteSummaryResponse? Entrante { get; set; }
}

public class ParticipanteSummaryResponse
{
    public int Id { get; set; }
    public int PerfilId { get; set; }
    public string NombreGuardia { get; set; }
    public string Rol { get; set; }
    public string Estado { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int TotalOk { get; set; }
    public int TotalNoOk { get; set; }
}

public class CategoriaChecklistDto
{
    public string Categoria { get; set; }
    public List<PuntoChecklistDto> Puntos { get; set; }
}

public class PuntoChecklistDto
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
}

public class GuardarRespuestaRequest
{
    [JsonPropertyName("puntoId")]
    public int PuntoId { get; set; }

    [JsonPropertyName("respuesta")]
    public bool Respuesta { get; set; }

    [JsonPropertyName("comentario")]
    public string? Comentario { get; set; }
}

public class CrearIncidenciaRequest
{
    [JsonPropertyName("relevoId")]
    public int RelevoId { get; set; }

    [JsonPropertyName("puntoId")]
    public int PuntoId { get; set; }

    [JsonPropertyName("tipoOrigen")]
    public string TipoOrigen { get; set; } = "NoOk";

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }

    [JsonPropertyName("fotoBase64")]
    public string? FotoBase64 { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

public class IncidenciaResponse
{
    public int Id { get; set; }
    public int RelevoId { get; set; }
    public DateOnly FechaRelevo { get; set; }
    public int PuntoId { get; set; }
    public string NombrePunto { get; set; } = string.Empty;
    public string CategoriaPunto { get; set; } = string.Empty;
    public string TipoOrigen { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? FotoBase64 { get; set; }  // O URL si se usa blob storage
    public string? MimeType { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? ResueltaPorId { get; set; }
    public string? ResueltaPorNombre { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? NotaResolucion { get; set; }
}

public class IniciarParticipanteRequest
{
    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }
}

public class CerrarParticipanteRequest
{
    public string FirmaBase64 { get; set; }
    public string? Observaciones { get; set; }
    public List<GuardarRespuestaRequest> Respuestas { get; set; }
}

public class HistorialRelevoItem
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public string NombreTurno { get; set; }
    public string Estado { get; set; }
    public string? NombreSaliente { get; set; }
    public string? NombreEntrante { get; set; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

public class MiActivoResponse
{
    [JsonPropertyName("relevo")]
    public RelevoHoyResponse Relevo { get; set; } = new();

    [JsonPropertyName("participanteId")]
    public int ParticipanteId { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("estadoParticipante")]
    public string EstadoParticipante { get; set; } = string.Empty;

    [JsonPropertyName("ventanaInicio")]
    public TimeOnly VentanaInicio { get; set; }

    [JsonPropertyName("ventanaFin")]
    public TimeOnly VentanaFin { get; set; }

    [JsonPropertyName("estaDentroVentana")]
    public bool EstaDentroVentana { get; set; }

    [JsonPropertyName("puedeIniciar")]
    public bool PuedeIniciar { get; set; }

    [JsonPropertyName("puedeCerrar")]
    public bool PuedeCerrar { get; set; }
}

public class ChecklistRespuestaResponse
{
    public int Id { get; set; }
    public int ParticipanteId { get; set; }
    public int PuntoId { get; set; }
    public string NombrePunto { get; set; } = string.Empty;
    public string CategoriaPunto { get; set; } = string.Empty;
    public bool Respuesta { get; set; }
    public string? Comentario { get; set; }
    public DateTime FechaRespuesta { get; set; }
}

public class ActualizarIncidenciaRequest
{
    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }

    [JsonPropertyName("fotoBase64")]
    public string? FotoBase64 { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

public class RelevoDetalleResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("configTurnoId")]
    public int ConfigTurnoId { get; set; }

    [JsonPropertyName("nombreTurno")]
    public string NombreTurno { get; set; } = string.Empty;

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("saliente")]
    public ParticipanteResponse? Saliente { get; set; }

    [JsonPropertyName("entrante")]
    public ParticipanteResponse? Entrante { get; set; }

    [JsonPropertyName("incidencias")]
    public List<IncidenciaResponse> Incidencias { get; set; } = new();

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("creadoPor")]
    public int? CreadoPor { get; set; }

    [JsonPropertyName("fechaModificacion")]
    public DateTime? FechaModificacion { get; set; }

    [JsonPropertyName("modificadoPor")]
    public int? ModificadoPor { get; set; }
}

public class ParticipanteResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("relevoId")]
    public int RelevoId { get; set; }

    [JsonPropertyName("perfilId")]
    public int PerfilId { get; set; }

    [JsonPropertyName("nombreGuardia")]
    public string NombreGuardia { get; set; } = string.Empty;

    [JsonPropertyName("numeroEmpleado")]
    public string? NumeroEmpleado { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("fechaInicio")]
    public DateTime? FechaInicio { get; set; }

    [JsonPropertyName("fechaFin")]
    public DateTime? FechaFin { get; set; }

    [JsonPropertyName("firmaBase64")]
    public string? FirmaBase64 { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }

    [JsonPropertyName("respuestas")]
    public List<ChecklistRespuestaResponse> Respuestas { get; set; } = new();

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("creadoPor")]
    public int? CreadoPor { get; set; }

    [JsonPropertyName("fechaModificacion")]
    public DateTime? FechaModificacion { get; set; }

    [JsonPropertyName("modificadoPor")]
    public int? ModificadoPor { get; set; }
}