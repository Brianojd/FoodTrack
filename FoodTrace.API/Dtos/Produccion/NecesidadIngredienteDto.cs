using FoodTrace.API.Dtos.Comunes;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Produccion;

public sealed class NecesidadIngredienteDto : EntidadEmpresaDto
{
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
