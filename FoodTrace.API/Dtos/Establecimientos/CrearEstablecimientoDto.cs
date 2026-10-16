using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Establecimientos;

public sealed class CrearEstablecimientoDto
{
    [JsonPropertyName("name")]
    [Required] public string Nombre { get; set; } = string.Empty;
}
