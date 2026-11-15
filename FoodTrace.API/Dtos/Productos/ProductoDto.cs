using FoodTrace.API.Dtos.Comunes;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Productos;

public sealed class ProductoDto : EntidadEmpresaDto
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("baseUnitId")]
    public string UnidadBaseId { get; set; } = string.Empty;
}
