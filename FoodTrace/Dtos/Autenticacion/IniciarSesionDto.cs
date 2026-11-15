using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Autenticacion;

public sealed class IniciarSesionDto
{
    [JsonPropertyName("email")]
    public string Correo { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Contrasena { get; set; } = string.Empty;
}
