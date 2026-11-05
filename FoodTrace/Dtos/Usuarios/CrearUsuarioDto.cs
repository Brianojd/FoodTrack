using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Usuarios;

public sealed class CrearUsuarioDto
{
    [JsonPropertyName("email")]
    public string Correo { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Contrasena { get; set; } = string.Empty;
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = [];
}
