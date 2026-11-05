using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Empresas;

public sealed class ActualizarEmpresaDto
{
    [Required]
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
}
