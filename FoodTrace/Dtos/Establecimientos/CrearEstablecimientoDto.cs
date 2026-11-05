using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Establecimientos;

public sealed class CrearEstablecimientoDto
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
}
