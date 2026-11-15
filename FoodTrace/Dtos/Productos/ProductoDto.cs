using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Productos;

public sealed class ProductoDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("baseUnitId")]
    public string UnidadBaseId { get; set; } = string.Empty;
}
