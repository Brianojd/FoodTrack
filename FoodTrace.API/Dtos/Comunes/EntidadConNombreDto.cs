using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Comunes;

public sealed class EntidadConNombreDto : EntidadEmpresaDto
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
}
