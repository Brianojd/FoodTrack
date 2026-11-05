using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Compras;

public sealed class ActualizarCantidadPresentacionesDto
{
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
    [JsonPropertyName("packCount")]
    public decimal CantidadPresentaciones { get; set; }
}
