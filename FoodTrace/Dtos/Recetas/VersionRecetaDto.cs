using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Recetas;

public sealed class VersionRecetaDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("familyId")]
    public Guid FamiliaRecetaId { get; set; }
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("outputProductId")]
    public Guid ProductoResultadoId { get; set; }
    [JsonPropertyName("number")]
    public int NumeroVersion { get; set; }
    [JsonPropertyName("yield")]
    public decimal Rendimiento { get; set; }
    [JsonPropertyName("yieldQuantity")]
    public decimal? CantidadRendimiento { get; set; }
    [JsonPropertyName("yieldUnitId")]
    public string? UnidadRendimientoId { get; set; }
    [JsonPropertyName("yieldPortions")]
    public decimal? PorcionesRendimiento { get; set; }
    [JsonPropertyName("instructions")]
    public string Instrucciones { get; set; } = string.Empty;
    [JsonPropertyName("published")]
    public bool Publicada { get; set; }
    [JsonPropertyName("ingredients")]
    public List<IngredienteRecetaDto> Ingredientes { get; set; } = [];
}
