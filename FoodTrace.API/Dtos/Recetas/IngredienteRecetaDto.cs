using FoodTrace.API.Dtos.Comunes;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Recetas;

public sealed class IngredienteRecetaDto : EntidadEmpresaDto
{
    [JsonPropertyName("recipeVersionId")]
    public Guid VersionRecetaId { get; set; }
    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }
    [JsonPropertyName("unitId")]
    public string UnidadMedidaId { get; set; } = string.Empty;
    [JsonPropertyName("quantity")]
    public decimal Cantidad { get; set; }
}
