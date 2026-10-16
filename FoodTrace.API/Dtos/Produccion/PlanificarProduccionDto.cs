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
    [Required] public string UnidadMedidaId { get; set; } = string.Empty;
    [JsonPropertyName("date")]
    public DateOnly FechaPlanificada { get; set; }
}
