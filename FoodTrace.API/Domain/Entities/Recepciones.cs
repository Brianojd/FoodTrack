namespace FoodTrace.API.Domain.Entities;

public sealed class Recepcion : EntidadEmpresa
{
    public Guid OrdenRecepcionId { get; set; }
    public string UsuarioReceptorId { get; set; } = "";
    public DateTimeOffset FechaRecepcion { get; set; }
    public string? Observacion { get; set; }
    public List<DetalleRecepcion> Detalles { get; set; } = [];
}

public sealed class DetalleRecepcion : EntidadEmpresa
{
    public Guid RecepcionId { get; set; }
    public Guid DetalleOrdenRecepcionId { get; set; }
    public Guid OrdenRecepcionId { get; set; }
    public Guid ProductoId { get; set; }
    public string NombreProducto { get; set; } = "";
    public Guid PresentacionProveedorId { get; set; }
    public string NombrePresentacion { get; set; } = "";
    public decimal CantidadPresentacionesEsperadas { get; set; }
    public decimal CantidadPresentacionesRecibidas { get; set; }
    public decimal CantidadPresentacionesFaltantes { get; set; }
    public decimal CantidadBasePresentacion { get; set; }
    public decimal CantidadEsperada { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal CantidadFaltante { get; set; }
    public string UnidadBaseId { get; set; } = "";
}

public sealed class IngresoInventario : EntidadEmpresa
{
    public Guid RecepcionId { get; set; }
    public Guid DetalleRecepcionId { get; set; }
    public Guid OrdenRecepcionId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid EstablecimientoId { get; set; }
    public decimal Cantidad { get; set; }
    public string UnidadBaseId { get; set; } = "";
    public DateTimeOffset FechaIngreso { get; set; }
}
