using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Produccion;

public sealed class NecesidadIngredienteDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
    [JsonPropertyName("productionOrderId")]
    public Guid OrdenProduccionId { get; set; }
    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }
    [JsonPropertyName("baseUnitId")]
    public string UnidadBaseId { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    public decimal CantidadNecesaria { get; set; }
    [JsonPropertyName("allocated")]
    public decimal CantidadAsignada { get; set; }
}
