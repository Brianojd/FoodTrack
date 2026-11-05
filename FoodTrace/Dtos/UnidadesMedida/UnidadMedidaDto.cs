using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.UnidadesMedida;

public sealed class UnidadMedidaDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = string.Empty;
    [JsonPropertyName("factor")]
    public decimal FactorConversion { get; set; }
}
