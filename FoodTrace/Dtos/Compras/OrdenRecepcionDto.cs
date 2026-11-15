using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Compras;

public sealed class OrdenRecepcionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
    [JsonPropertyName("purchaseId")]
    public Guid PedidoCompraId { get; set; }
    [JsonPropertyName("supplierId")]
    public Guid ProveedorId { get; set; }
    [JsonPropertyName("supplierName")]
    public string NombreProveedor { get; set; } = string.Empty;
    [JsonPropertyName("establishmentId")]
    public Guid EstablecimientoId { get; set; }
    [JsonPropertyName("establishmentName")]
    public string NombreEstablecimiento { get; set; } = string.Empty;
    [JsonPropertyName("createdById")]
    public string? CreadoPorId { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTimeOffset FechaCreacion { get; set; }
    [JsonPropertyName("status")]
    public int Estado { get; set; }
    [JsonPropertyName("lines")]
    public List<DetalleOrdenRecepcionDto> Detalles { get; set; } = [];
}
