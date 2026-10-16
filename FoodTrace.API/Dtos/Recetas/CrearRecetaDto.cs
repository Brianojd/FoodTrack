using System.ComponentModel.DataAnnotations;
using FoodTrace.API.Infrastructure.Validation;

using System.Text.Json.Serialization;
using FoodTrace.API.Dtos.Recetas;

namespace FoodTrace.API.Dtos.Recetas;

public sealed class CrearRecetaDto
{
    [JsonPropertyName("name")]
    [Required] public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("outputProductId")]
    public Guid ProductoResultadoId { get; set; }
    [JsonPropertyName("yield")]
    public decimal Rendimiento { get; set; }
    [JsonPropertyName("instructions")]
    [Required] public string Instrucciones { get; set; } = string.Empty;
    [JsonPropertyName("ingredients")]
    [ColeccionObligatoria] public IngredienteRecetaEntradaDto[] Ingredientes { get; set; } = [];
    [JsonPropertyName("revision")]
    public Guid? Revision { get; set; }
}
