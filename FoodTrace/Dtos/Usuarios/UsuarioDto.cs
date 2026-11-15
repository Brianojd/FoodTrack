using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Usuarios;

public sealed class UsuarioDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string Correo { get; set; } = string.Empty;
    [JsonPropertyName("enabled")]
    public bool Habilitado { get; set; }
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = [];
}
