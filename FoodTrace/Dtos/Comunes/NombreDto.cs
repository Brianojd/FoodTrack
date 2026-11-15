using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Comunes;

public sealed class NombreDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
}
