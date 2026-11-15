using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Proveedores;

public sealed class CrearProveedorDto
{
    [JsonPropertyName("name")]
    [Required] public string Nombre { get; set; } = string.Empty;
}
