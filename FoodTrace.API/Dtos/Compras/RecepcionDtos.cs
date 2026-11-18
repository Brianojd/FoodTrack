using System.Text.Json.Serialization;
using FoodTrace.API.Dtos.Comunes;

namespace FoodTrace.API.Dtos.Compras;

public sealed class ConfirmarRecepcionDto
{
    [JsonPropertyName("revision")] public Guid Revision { get; set; }
    [JsonPropertyName("details")] public List<DetalleRecepcionEntradaDto>? Detalles { get; set; }
    [JsonPropertyName("observation")] public string? Observacion { get; set; }
}

public sealed class DetalleRecepcionEntradaDto
{
    [JsonPropertyName("receiptOrderLineId")] public Guid DetalleOrdenRecepcionId { get; set; }
    [JsonPropertyName("receivedPackCount")] public decimal CantidadPresentacionesRecibidas { get; set; }
}

public sealed class RecepcionDto : EntidadEmpresaDto
{
    [JsonPropertyName("receiptOrderId")] public Guid OrdenRecepcionId { get; set; }
    [JsonPropertyName("receiverId")] public string UsuarioReceptorId { get; set; } = "";
    [JsonPropertyName("receivedAt")] public DateTimeOffset FechaRecepcion { get; set; }
    [JsonPropertyName("observation")] public string? Observacion { get; set; }
    [JsonPropertyName("details")] public List<DetalleRecepcionDto> Detalles { get; set; } = [];
}

public sealed class DetalleRecepcionDto : EntidadEmpresaDto
{
    [JsonPropertyName("receiptOrderLineId")] public Guid DetalleOrdenRecepcionId { get; set; }
    [JsonPropertyName("productId")] public Guid ProductoId { get; set; }
    [JsonPropertyName("productName")] public string NombreProducto { get; set; } = "";
    [JsonPropertyName("supplierPackId")] public Guid PresentacionProveedorId { get; set; }
    [JsonPropertyName("packName")] public string NombrePresentacion { get; set; } = "";
    [JsonPropertyName("expectedPackCount")] public decimal CantidadPresentacionesEsperadas { get; set; }
    [JsonPropertyName("receivedPackCount")] public decimal CantidadPresentacionesRecibidas { get; set; }
    [JsonPropertyName("missingPackCount")] public decimal CantidadPresentacionesFaltantes { get; set; }
    [JsonPropertyName("packBaseAmount")] public decimal CantidadBasePresentacion { get; set; }
    [JsonPropertyName("expectedAmount")] public decimal CantidadEsperada { get; set; }
    [JsonPropertyName("receivedAmount")] public decimal CantidadRecibida { get; set; }
    [JsonPropertyName("missingAmount")] public decimal CantidadFaltante { get; set; }
    [JsonPropertyName("baseUnitId")] public string UnidadBaseId { get; set; } = "";
}

public sealed class DiferenciaRecepcionDto
{
    [JsonPropertyName("receiptId")] public Guid RecepcionId { get; set; }
    [JsonPropertyName("receiptOrderId")] public Guid OrdenRecepcionId { get; set; }
    [JsonPropertyName("purchaseId")] public Guid PedidoCompraId { get; set; }
    [JsonPropertyName("supplierName")] public string NombreProveedor { get; set; } = "";
    [JsonPropertyName("establishmentName")] public string NombreEstablecimiento { get; set; } = "";
    [JsonPropertyName("receivedAt")] public DateTimeOffset FechaRecepcion { get; set; }
    [JsonPropertyName("receiverId")] public string UsuarioReceptorId { get; set; } = "";
    [JsonPropertyName("receiverEmail")] public string? CorreoReceptor { get; set; }
    [JsonPropertyName("productName")] public string NombreProducto { get; set; } = "";
    [JsonPropertyName("packName")] public string NombrePresentacion { get; set; } = "";
    [JsonPropertyName("expectedAmount")] public decimal CantidadEsperada { get; set; }
    [JsonPropertyName("receivedAmount")] public decimal CantidadRecibida { get; set; }
    [JsonPropertyName("missingAmount")] public decimal CantidadFaltante { get; set; }
    [JsonPropertyName("baseUnitId")] public string UnidadBaseId { get; set; } = "";
    [JsonPropertyName("observation")] public string? Observacion { get; set; }
}

public sealed class DiferenciasRecepcionPaginadasDto
{
    [JsonPropertyName("items")] public List<DiferenciaRecepcionDto> Items { get; set; } = [];
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("pageSize")] public int PageSize { get; set; }
}

public sealed class DiferenciasRecepcionQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public Guid? SupplierId { get; set; }
    public Guid? EstablishmentId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
