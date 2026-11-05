using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Compras;

public sealed class AsignacionCompraDto
{
    [JsonPropertyName("requirementId")]
    public Guid NecesidadIngredienteId { get; set; }
    [JsonPropertyName("amount")]
    public decimal CantidadAsignada { get; set; }
    [JsonPropertyName("released")]
    public bool Liberada { get; set; }
}
