using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Usuarios;

public sealed class ActualizarMembresiaDto
{
    [JsonPropertyName("enabled")]
    public bool Habilitado { get; set; }
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = [];
}
