using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Compras;

public sealed class DetallePedidoCompraDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }
    [JsonPropertyName("packName")]
    public string NombrePresentacion { get; set; } = string.Empty;
    [JsonPropertyName("baseUnitId")]
    public string UnidadBaseId { get; set; } = string.Empty;
    [JsonPropertyName("packBaseAmount")]
    public decimal CantidadBasePresentacion { get; set; }
    [JsonPropertyName("packCount")]
    public decimal CantidadPresentaciones { get; set; }
    [JsonPropertyName("orderedAmount")]
    public decimal CantidadPedida { get; set; }
    [JsonPropertyName("grossNeed")]
    public decimal NecesidadBruta { get; set; }
    [JsonPropertyName("allocations")]
    public List<AsignacionCompraDto> Asignaciones { get; set; } = [];
}
