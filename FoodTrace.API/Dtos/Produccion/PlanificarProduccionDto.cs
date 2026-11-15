using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Produccion;

public sealed class PlanificarProduccionDto
{
    [JsonPropertyName("establishmentId")]
    public Guid EstablecimientoId { get; set; }
    [JsonPropertyName("recipeVersionId")]
    public Guid VersionRecetaId { get; set; }
    [JsonPropertyName("quantity")]
    public decimal CantidadPlanificada { get; set; }
    [JsonPropertyName("unitId")]
    public string UnidadMedidaId { get; set; } = string.Empty;
    [JsonPropertyName("byPortions")]
    public bool PorPorciones { get; set; }
    [JsonPropertyName("date")]
    public DateOnly FechaPlanificada { get; set; }
}
