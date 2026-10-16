using FoodTrace.API.Dtos.Comunes;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Recetas;

public sealed class VersionRecetaDto : EntidadEmpresaDto
{
    [JsonPropertyName("familyId")]
    public Guid FamiliaRecetaId { get; set; }
    [JsonPropertyName("number")]
    public int NumeroVersion { get; set; }
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("outputProductId")]
    public Guid ProductoResultadoId { get; set; }
    [JsonPropertyName("yield")]
    public decimal Rendimiento { get; set; }
    [JsonPropertyName("instructions")]
    public string Instrucciones { get; set; } = string.Empty;
    [JsonPropertyName("published")]
    public bool Publicada { get; set; }
    [JsonPropertyName("ingredients")]
    public List<IngredienteRecetaDto> Ingredientes { get; set; } = [];
}
