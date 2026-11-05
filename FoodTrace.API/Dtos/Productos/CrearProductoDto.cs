using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Productos;

public sealed class CrearProductoDto
{
    [JsonPropertyName("name")]
    [Required] public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("baseUnitId")]
    [Required] public string UnidadBaseId { get; set; } = string.Empty;
}
