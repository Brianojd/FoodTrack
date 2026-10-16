using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Compras;

public sealed class CrearPedidoCompraDto
{
    [JsonPropertyName("supplierId")]
    public Guid ProveedorId { get; set; }
}
