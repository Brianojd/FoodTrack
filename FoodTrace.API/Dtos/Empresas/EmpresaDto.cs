using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Empresas;

public sealed class EmpresaDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
}
