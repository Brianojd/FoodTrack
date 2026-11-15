using FoodTrace.API.Infrastructure.Validation;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Compras;

public sealed class AgregarDetalleCompraDto
{
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
    [JsonPropertyName("supplierPackId")]
    public Guid PresentacionProveedorId { get; set; }
    [JsonPropertyName("packCount")]
    public decimal CantidadPresentaciones { get; set; }
    [JsonPropertyName("allocations")]
    public AsignacionNecesidadEntradaDto[] Asignaciones { get; set; } = [];
}
