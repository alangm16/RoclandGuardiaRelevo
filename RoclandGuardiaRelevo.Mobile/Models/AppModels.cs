using System.Text.Json.Serialization;

namespace RoclandGuardiaRelevo.Mobile.Models;

// ======================== AUTENTICACIÓN ========================
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
    [JsonPropertyName("superAdminUsuarioId")]
    public int SuperAdminUsuarioId { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;
    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } = string.Empty;
    [JsonPropertyName("nivelRol")]
    public int NivelRol { get; set; }
    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;
    [JsonPropertyName("turno")]          // Campo que debe devolver el backend: "Diurno" o "Nocturno"
    public string? Turno { get; set; }
    [JsonPropertyName("numeroEmpleado")]
    public string? NumeroEmpleado { get; set; }
}

// ======================== PUNTOS DEL CATÁLOGO ========================
public class PuntoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
    [JsonPropertyName("orden")]
    public int Orden { get; set; }
}

// Para agrupar en la UI por categoría
public class CategoriaChecklistDto
{
    public string Categoria { get; set; } = string.Empty;
    public List<PuntoDto> Puntos { get; set; } = new();
}

// ======================== CHECKLIST (RONDÍN) ========================
public class ChecklistPuntoItemDto
{
    [JsonPropertyName("idPunto")]
    public int IdPunto { get; set; }
    [JsonPropertyName("estado")]
    public bool Estado { get; set; }   // true = OK, false = Problema
}

public class GuardarChecklistDto
{
    [JsonPropertyName("tipoRondin")]
    public string TipoRondin { get; set; } = string.Empty;   // AMS, BME, AVS, BVE
    [JsonPropertyName("observacion")]
    public string? Observacion { get; set; }
    [JsonPropertyName("firma")]
    public byte[]? Firma { get; set; }
    [JsonPropertyName("puntos")]
    public List<ChecklistPuntoItemDto> Puntos { get; set; } = new();
}

public class GuardarChecklistResponseDto
{
    [JsonPropertyName("idChecklist")]
    public int IdChecklist { get; set; }
    [JsonPropertyName("incidenciasGeneradas")]
    public int IncidenciasGeneradas { get; set; }
}

public class ChecklistResumenDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("fechaHoraLocal")]
    public DateTime FechaHoraLocal { get; set; }
    [JsonPropertyName("tipoRondin")]
    public string TipoRondin { get; set; } = string.Empty;
    [JsonPropertyName("descripcionRondin")]
    public string DescripcionRondin { get; set; } = string.Empty;
    [JsonPropertyName("idGuardia")]
    public int IdGuardia { get; set; }
    [JsonPropertyName("guardia")]
    public string Guardia { get; set; } = string.Empty;
    [JsonPropertyName("todoOk")]
    public bool TodoOk { get; set; }
    [JsonPropertyName("tieneFirma")]
    public bool TieneFirma { get; set; }
}

public class ChecklistDetalleDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("fechaHoraLocal")]
    public DateTime FechaHoraLocal { get; set; }
    [JsonPropertyName("tipoRondin")]
    public string TipoRondin { get; set; } = string.Empty;
    [JsonPropertyName("descripcionRondin")]
    public string DescripcionRondin { get; set; } = string.Empty;
    [JsonPropertyName("idGuardia")]
    public int IdGuardia { get; set; }
    [JsonPropertyName("guardia")]
    public string Guardia { get; set; } = string.Empty;
    [JsonPropertyName("observacion")]
    public string? Observacion { get; set; }
    [JsonPropertyName("tieneFirma")]
    public bool TieneFirma { get; set; }
    [JsonPropertyName("puntos")]
    public List<ChecklistPuntoDetalleDto> Puntos { get; set; } = new();
}

public class ChecklistPuntoDetalleDto
{
    [JsonPropertyName("idPunto")]
    public int IdPunto { get; set; }
    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
    [JsonPropertyName("orden")]
    public int Orden { get; set; }
    [JsonPropertyName("estado")]
    public bool Estado { get; set; }
}

// ======================== INCIDENCIAS ========================
public class IncidenciaDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("fechaDeteccionLocal")]
    public DateTime FechaDeteccionLocal { get; set; }
    [JsonPropertyName("idChecklistSaliente")]
    public int IdChecklistSaliente { get; set; }
    [JsonPropertyName("tipoRondinSaliente")]
    public string TipoRondinSaliente { get; set; } = string.Empty;
    [JsonPropertyName("guardiaSaliente")]
    public string GuardiaSaliente { get; set; } = string.Empty;
    [JsonPropertyName("fechaSaliente")]
    public DateTime FechaSaliente { get; set; }
    [JsonPropertyName("idChecklistEntrante")]
    public int IdChecklistEntrante { get; set; }
    [JsonPropertyName("tipoRondinEntrante")]
    public string TipoRondinEntrante { get; set; } = string.Empty;
    [JsonPropertyName("guardiaEntrante")]
    public string GuardiaEntrante { get; set; } = string.Empty;
    [JsonPropertyName("fechaEntrante")]
    public DateTime FechaEntrante { get; set; }
    [JsonPropertyName("idPunto")]
    public int IdPunto { get; set; }
    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;
    [JsonPropertyName("punto")]
    public string Punto { get; set; } = string.Empty;
    [JsonPropertyName("descripcionPunto")]
    public string? DescripcionPunto { get; set; }
    [JsonPropertyName("resuelta")]
    public bool Resuelta { get; set; }
    [JsonPropertyName("fechaResolucionLocal")]
    public DateTime? FechaResolucionLocal { get; set; }
    public string GuardiaComparacion =>
    $"{GuardiaSaliente} (saliente) vs {GuardiaEntrante} (entrante)";
}

// ======================== FOTOS ========================
public class AgregarFotoDto
{
    [JsonPropertyName("idChecklist")]
    public int IdChecklist { get; set; }
    [JsonPropertyName("foto")]
    public byte[] Foto { get; set; } = Array.Empty<byte>();
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "image/jpeg";
}

// ======================== UTILIDADES ========================
public class PagedResult<T>
{
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    [JsonPropertyName("totalRegistros")]
    public int TotalRegistros { get; set; }
    [JsonPropertyName("paginaActual")]
    public int PaginaActual { get; set; }
    [JsonPropertyName("registrosPorPagina")]
    public int RegistrosPorPagina { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRegistros / RegistrosPorPagina);
    public bool HasNext => PaginaActual < TotalPages;
    public bool HasPrev => PaginaActual > 1;
}

public class EstadoRondinDto
{
    public string TipoRondin { get; set; } = string.Empty;
    public bool Existe { get; set; }
    public int? IdChecklist { get; set; }
    public int? IdGuardiaQueLo { get; set; }
    public bool YoLoHice { get; set; }
}

public class EstadoDiaDto
{
    public List<EstadoRondinDto> Rondines { get; set; } = new();
}