using System.Text.Json.Serialization;
using FoodTrace.Dtos.Recetas;

namespace FoodTrace.Dtos.Recetas;

public sealed class ActualizarRecetaDto
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("outputProductId")]
    public Guid ProductoResultadoId { get; set; }
    [JsonPropertyName("yield")]
    public decimal Rendimiento { get; set; }
    [JsonPropertyName("yieldUnitId")]
    public string? UnidadRendimientoId { get; set; }
    [JsonPropertyName("yieldPortions")]
    public decimal? PorcionesRendimiento { get; set; }
    [JsonPropertyName("instructions")]
    public string Instrucciones { get; set; } = string.Empty;
    [JsonPropertyName("ingredients")]
    public IngredienteRecetaEntradaDto[] Ingredientes { get; set; } = [];
    [JsonPropertyName("revision")]
    public Guid? Revision { get; set; }
}
