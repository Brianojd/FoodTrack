using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Compras;

public sealed class CrearPedidoCompraDto
{
    [JsonPropertyName("supplierId")]
    public Guid ProveedorId { get; set; }
}
