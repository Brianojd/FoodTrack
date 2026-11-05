using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Productos;

public sealed class CrearProductoDto
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("baseUnitId")]
    public string UnidadBaseId { get; set; } = string.Empty;
}
