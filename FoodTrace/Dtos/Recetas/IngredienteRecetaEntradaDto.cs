using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Recetas;

public sealed class IngredienteRecetaEntradaDto
{
    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }
    [JsonPropertyName("unitId")]
    public string UnidadMedidaId { get; set; } = string.Empty;
    [JsonPropertyName("quantity")]
    public decimal Cantidad { get; set; }
}
