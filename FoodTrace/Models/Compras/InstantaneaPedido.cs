using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Produccion;

namespace FoodTrace.Models.Compras;

// View snapshots only: never change server DTO contracts or adopt a fresh revision implicitly.
public static class InstantaneaPedido
{
    public static PedidoCompraDto Copiar(PedidoCompraDto pedido) => new()
    {
        Id = pedido.Id, Revision = pedido.Revision, ProveedorId = pedido.ProveedorId, Estado = pedido.Estado,
        CreadoPorId = pedido.CreadoPorId, FechaCreacion = pedido.FechaCreacion,
        EnviadoPorId = pedido.EnviadoPorId, FechaEnvio = pedido.FechaEnvio,
        DecididoPorId = pedido.DecididoPorId, FechaDecision = pedido.FechaDecision, MotivoDecision = pedido.MotivoDecision,
        Detalles = pedido.Detalles.Select(d => new DetallePedidoCompraDto
        {
            Id = d.Id, ProductoId = d.ProductoId, NombrePresentacion = d.NombrePresentacion, UnidadBaseId = d.UnidadBaseId,
            CantidadBasePresentacion = d.CantidadBasePresentacion, CantidadPresentaciones = d.CantidadPresentaciones,
            CantidadPedida = d.CantidadPedida, NecesidadBruta = d.NecesidadBruta,
            Asignaciones = d.Asignaciones.Select(a => new AsignacionCompraDto
            {
                NecesidadIngredienteId = a.NecesidadIngredienteId, CantidadAsignada = a.CantidadAsignada, Liberada = a.Liberada
            }).ToList()
        }).ToList()
    };

    public static List<NecesidadIngredienteDto> CopiarNecesidades(IEnumerable<NecesidadIngredienteDto> necesidades) => necesidades.Select(n => new NecesidadIngredienteDto
    {
        Id = n.Id, Revision = n.Revision, OrdenProduccionId = n.OrdenProduccionId, ProductoId = n.ProductoId,
        UnidadBaseId = n.UnidadBaseId, CantidadNecesaria = n.CantidadNecesaria, CantidadAsignada = n.CantidadAsignada
    }).ToList();

    public static DetallePedidoCompraDto? DetalleActivo(PedidoCompraDto pedido, Guid id) => pedido.Estado == 0
        ? pedido.Detalles.FirstOrDefault(d => d.Id == id && d.Asignaciones.Any(a => !a.Liberada)) : null;
}
