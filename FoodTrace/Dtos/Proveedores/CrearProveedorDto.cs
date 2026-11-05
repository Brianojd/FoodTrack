using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Proveedores;

public sealed class CrearProveedorDto
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
}
