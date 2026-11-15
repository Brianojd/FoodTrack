using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Compras;

public sealed class CrearPedidoCompraDto
{
    [JsonPropertyName("supplierId")]
    public Guid ProveedorId { get; set; }
    [JsonPropertyName("establishmentId")]
    public Guid EstablecimientoId { get; set; }
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
}
