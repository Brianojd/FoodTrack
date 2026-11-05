using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Usuarios;

public sealed class UsuarioDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string? Correo { get; set; }
    [JsonPropertyName("enabled")]
    public bool Habilitado { get; set; }
    [JsonPropertyName("roles")]
    public IList<string> Roles { get; set; } = new List<string>();
}
