using System.Globalization;
using System.Text.Json.Serialization;

namespace FoodTrace.Mobile.Models.Recepcion;

public enum EstadoOrdenRecepcion
{
    Pendiente = 0,
    Cerrada = 1
}

public sealed class OrdenRecepcionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("companyId")]
    public Guid EmpresaId { get; set; }

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

    [JsonIgnore]
    public string CodigoCorto => $"Orden #{Id.ToString("N")[..8].ToUpperInvariant()}";

    [JsonIgnore]
    public string EstadoTexto => Estado == (int)EstadoOrdenRecepcion.Cerrada ? "Cerrada" : "Pendiente";

    [JsonIgnore]
    public string DestinoResumen => $"Destino: {NombreEstablecimiento}";

    [JsonIgnore]
    public string FechaResumen => $"Creada: {FechaCreacion.ToLocalTime():dd/MM/yyyy HH:mm}";

    [JsonIgnore]
    public string LineasResumen => Detalles.Count == 1
        ? "1 línea esperada"
        : $"{Detalles.Count} líneas esperadas";
}

public sealed class DetalleOrdenRecepcionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("companyId")]
    public Guid EmpresaId { get; set; }

    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }

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

    [JsonIgnore]
    public string PresentacionResumen =>
        $"{NombrePresentacion} ({FormatoCantidad.Formatear(CantidadBasePresentacion)} {UnidadBaseId} c/u)";

    [JsonIgnore]
    public string EsperadoResumen =>
        $"Esperado: {FormatoCantidad.Formatear(CantidadPresentaciones)} pres. ({FormatoCantidad.Formatear(CantidadEsperada)} {UnidadBaseId})";
}

public sealed class ConfirmarRecepcionDto
{
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }

    [JsonPropertyName("details")]
    public List<DetalleRecepcionEntradaDto> Detalles { get; set; } = [];

    [JsonPropertyName("observation")]
    public string? Observacion { get; set; }
}

public sealed class DetalleRecepcionEntradaDto
{
    [JsonPropertyName("receiptOrderLineId")]
    public Guid DetalleOrdenRecepcionId { get; set; }

    [JsonPropertyName("receivedPackCount")]
    public decimal CantidadPresentacionesRecibidas { get; set; }
}

public sealed class RecepcionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("companyId")]
    public Guid EmpresaId { get; set; }

    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }

    [JsonPropertyName("receiptOrderId")]
    public Guid OrdenRecepcionId { get; set; }

    [JsonPropertyName("receiverId")]
    public string UsuarioReceptorId { get; set; } = string.Empty;

    [JsonPropertyName("receivedAt")]
    public DateTimeOffset FechaRecepcion { get; set; }

    [JsonPropertyName("observation")]
    public string? Observacion { get; set; }

    [JsonPropertyName("details")]
    public List<DetalleRecepcionDto> Detalles { get; set; } = [];
}

public sealed class DetalleRecepcionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("companyId")]
    public Guid EmpresaId { get; set; }

    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }

    [JsonPropertyName("receiptOrderLineId")]
    public Guid DetalleOrdenRecepcionId { get; set; }

    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }

    [JsonPropertyName("productName")]
    public string NombreProducto { get; set; } = string.Empty;

    [JsonPropertyName("supplierPackId")]
    public Guid PresentacionProveedorId { get; set; }

    [JsonPropertyName("packName")]
    public string NombrePresentacion { get; set; } = string.Empty;

    [JsonPropertyName("expectedPackCount")]
    public decimal CantidadPresentacionesEsperadas { get; set; }

    [JsonPropertyName("receivedPackCount")]
    public decimal CantidadPresentacionesRecibidas { get; set; }

    [JsonPropertyName("missingPackCount")]
    public decimal CantidadPresentacionesFaltantes { get; set; }

    [JsonPropertyName("packBaseAmount")]
    public decimal CantidadBasePresentacion { get; set; }

    [JsonPropertyName("expectedAmount")]
    public decimal CantidadEsperada { get; set; }

    [JsonPropertyName("receivedAmount")]
    public decimal CantidadRecibida { get; set; }

    [JsonPropertyName("missingAmount")]
    public decimal CantidadFaltante { get; set; }

    [JsonPropertyName("baseUnitId")]
    public string UnidadBaseId { get; set; } = string.Empty;

    [JsonIgnore]
    public string EsperadoTexto =>
        $"{FormatoCantidad.Formatear(CantidadPresentacionesEsperadas)} pres. ({FormatoCantidad.Formatear(CantidadEsperada)} {UnidadBaseId})";

    [JsonIgnore]
    public string RecibidoTexto =>
        $"{FormatoCantidad.Formatear(CantidadPresentacionesRecibidas)} pres. ({FormatoCantidad.Formatear(CantidadRecibida)} {UnidadBaseId})";

    [JsonIgnore]
    public string FaltanteTexto =>
        $"{FormatoCantidad.Formatear(CantidadPresentacionesFaltantes)} pres. ({FormatoCantidad.Formatear(CantidadFaltante)} {UnidadBaseId})";

    [JsonIgnore]
    public string IngresoInventarioTexto => CantidadRecibida > 0
        ? $"Ingresa a inventario: {FormatoCantidad.Formatear(CantidadRecibida)} {UnidadBaseId}"
        : "Recibido en 0: no genera ingreso de inventario";
}

public static class FormatoCantidad
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public static string Formatear(decimal valor) =>
        valor == decimal.Truncate(valor)
            ? valor.ToString("0", Cultura)
            : valor.ToString("0.######", Cultura);
}
