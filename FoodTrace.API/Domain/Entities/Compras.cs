using FoodTrace.API.Domain.Enums;

namespace FoodTrace.API.Domain.Entities;

public sealed class PedidoCompra : EntidadEmpresa
{
    public Guid ProveedorId { get; set; }
    public EstadoPedidoCompra Estado { get; set; }
    public string CreadoPorId { get; set; } = "";
    public DateTimeOffset FechaCreacion { get; set; }
    public string? EnviadoPorId { get; set; }
    public DateTimeOffset? FechaEnvio { get; set; }
    public string? DecididoPorId { get; set; }
    public DateTimeOffset? FechaDecision { get; set; }
    public string? MotivoDecision { get; set; }
    public List<DetallePedidoCompra> Detalles { get; set; } = [];
}

public sealed class DetallePedidoCompra : EntidadEmpresa
{
    public Guid PedidoCompraId { get; set; }
    public Guid PresentacionProveedorId { get; set; }
    public Guid ProductoId { get; set; }
    public string NombrePresentacion { get; set; } = "";
    public string UnidadBaseId { get; set; } = "";
    public decimal CantidadBasePresentacion { get; set; }
    public decimal CantidadPresentaciones { get; set; }
    public decimal CantidadPedida { get; set; }
    public decimal NecesidadBruta { get; set; }
    public List<AsignacionCompra> Asignaciones { get; set; } = [];
}

public sealed class AsignacionCompra : EntidadEmpresa
{
    public Guid DetallePedidoCompraId { get; set; }
    public Guid NecesidadIngredienteId { get; set; }
    public decimal CantidadAsignada { get; set; }
    public bool Liberada { get; set; }
}
