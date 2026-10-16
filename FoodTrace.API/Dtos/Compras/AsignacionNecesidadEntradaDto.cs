using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Compras;

public sealed class AsignacionNecesidadEntradaDto
{
    [JsonPropertyName("requirementId")]
    public Guid NecesidadIngredienteId { get; set; }
    [JsonPropertyName("amount")]
    public decimal CantidadAsignada { get; set; }
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
}
