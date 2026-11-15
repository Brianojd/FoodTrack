using System.Text.Json.Serialization;
using FoodTrace.API.Dtos.Comunes;

namespace FoodTrace.API.Dtos.Compras;

public sealed class DetalleOrdenRecepcionDto : EntidadEmpresaDto
{
    [JsonPropertyName("receiptOrderId")]
    public Guid OrdenRecepcionId { get; set; }
    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }
    [JsonPropertyName("productName")]
    public string NombreProducto { get; set; } = string.Empty;
    [JsonPropertyName("supplierPackId")]
    public Guid PresentacionProveedorId { get; set; }
    [JsonPropertyName("packName")]
    public string NombrePresentacion { get; set; } = string.Empty;
    [JsonPropertyName("packCount")]
    public decimal CantidadPresentaciones { get; set; }
    [JsonPropertyName("packBaseAmount")]
    public decimal CantidadBasePresentacion { get; set; }
    [JsonPropertyName("expectedAmount")]
    public decimal CantidadEsperada { get; set; }
    [JsonPropertyName("baseUnitId")]
    public string UnidadBaseId { get; set; } = string.Empty;
}
