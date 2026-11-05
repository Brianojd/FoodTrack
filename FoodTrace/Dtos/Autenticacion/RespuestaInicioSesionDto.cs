using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Autenticacion;

public sealed class RespuestaInicioSesionDto
{
    [JsonPropertyName("tokenType")]
    public string TipoToken { get; set; } = string.Empty;
    [JsonPropertyName("accessToken")]
    public string TokenAcceso { get; set; } = string.Empty;
    [JsonPropertyName("expiresIn")]
    public long ExpiraEn { get; set; }
    [JsonPropertyName("refreshToken")]
    public string TokenRenovacion { get; set; } = string.Empty;
}
