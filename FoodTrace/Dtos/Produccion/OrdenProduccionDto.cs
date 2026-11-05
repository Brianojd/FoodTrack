using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Produccion;

public sealed class OrdenProduccionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("recipeVersionId")]
    public Guid VersionRecetaId { get; set; }
    [JsonPropertyName("establishmentId")]
    public Guid EstablecimientoId { get; set; }
    [JsonPropertyName("quantity")]
    public decimal CantidadPlanificada { get; set; }
    [JsonPropertyName("date")]
    public DateOnly FechaPlanificada { get; set; }
    [JsonPropertyName("createdBy")]
    public string CreadoPorId { get; set; } = string.Empty;
    [JsonPropertyName("createdAt")]
    public DateTimeOffset FechaCreacion { get; set; }
    [JsonPropertyName("requirements")]
    public List<NecesidadIngredienteDto> Necesidades { get; set; } = [];
}
