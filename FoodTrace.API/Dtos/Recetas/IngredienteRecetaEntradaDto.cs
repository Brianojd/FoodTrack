using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Recetas;

public sealed class IngredienteRecetaEntradaDto
{
    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }
    [JsonPropertyName("unitId")]
    [Required] public string UnidadMedidaId { get; set; } = string.Empty;
    [JsonPropertyName("quantity")]
    public decimal Cantidad { get; set; }
}
