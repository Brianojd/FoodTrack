using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Empresas;

public sealed class ActualizarEmpresaDto
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
}
