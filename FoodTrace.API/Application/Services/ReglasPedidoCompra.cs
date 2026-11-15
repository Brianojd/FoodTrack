using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Domain.Enums;

namespace FoodTrace.API.Application.Services;

public static class ReglasPedidoCompra
{
    public static void Transicionar(this PedidoCompra pedido, string accion, string actorId, DateTimeOffset fecha, string? motivo)
    {
        switch (accion)
        {
            case "submit" when pedido.Estado == EstadoPedidoCompra.Borrador && pedido.EstablecimientoId is not null && pedido.EstablecimientoId != Guid.Empty && DetallesActivos(pedido).Any():
                pedido.Estado = EstadoPedidoCompra.Enviado; pedido.EnviadoPorId = actorId; pedido.FechaEnvio = fecha; break;
            default: throw new ExcepcionRegla("Invalid purchase transition.");
        }
    }

    public static IEnumerable<DetallePedidoCompra> DetallesActivos(PedidoCompra pedido) =>
        pedido.Detalles.Where(x => x.Asignaciones.Count == 0 || x.Asignaciones.Any(a => !a.Liberada));

    public static void AsegurarBorrador(PedidoCompra pedido, string mensaje = "Only draft purchases can be edited.")
    {
        if (pedido.Estado != EstadoPedidoCompra.Borrador) throw new ExcepcionRegla(mensaje);
    }

    // Aprobado/Rechazado quedan como valores históricos de filas existentes; ninguna transición nueva los alcanza.
    public static bool EsTransicionPersistidaValida(EstadoPedidoCompra anterior, EstadoPedidoCompra actual) =>
        anterior == actual || anterior == EstadoPedidoCompra.Borrador && actual == EstadoPedidoCompra.Enviado;
}
