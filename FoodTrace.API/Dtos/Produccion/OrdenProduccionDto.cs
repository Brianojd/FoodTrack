using FoodTrace.API.Dtos.Comunes;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Produccion;

public sealed class OrdenProduccionDto : EntidadEmpresaDto
{
    [JsonPropertyName("establishmentId")]
    public Guid EstablecimientoId { get; set; }
    [JsonPropertyName("recipeVersionId")]
    public Guid VersionRecetaId { get; set; }
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
