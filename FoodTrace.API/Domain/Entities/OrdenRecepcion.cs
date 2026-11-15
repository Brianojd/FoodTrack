using FoodTrace.API.Domain.Enums;

namespace FoodTrace.API.Domain.Entities;

public sealed class OrdenRecepcion : EntidadEmpresa
{
    public Guid PedidoCompraId { get; set; }
    public Guid ProveedorId { get; set; }
    public string NombreProveedor { get; set; } = "";
    public Guid EstablecimientoId { get; set; }
    public string NombreEstablecimiento { get; set; } = "";
    public string? CreadoPorId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public EstadoOrdenRecepcion Estado { get; set; }
    public List<DetalleOrdenRecepcion> Detalles { get; set; } = [];
}

public sealed class DetalleOrdenRecepcion : EntidadEmpresa
{
    public Guid OrdenRecepcionId { get; set; }
    public Guid ProductoId { get; set; }
    public string NombreProducto { get; set; } = "";
    public Guid PresentacionProveedorId { get; set; }
    public string NombrePresentacion { get; set; } = "";
    public decimal CantidadPresentaciones { get; set; }
    public decimal CantidadBasePresentacion { get; set; }
    public decimal CantidadEsperada { get; set; }
    public string UnidadBaseId { get; set; } = "";
}
