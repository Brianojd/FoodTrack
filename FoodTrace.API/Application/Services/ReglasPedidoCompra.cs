using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Domain.Enums;

namespace FoodTrace.API.Application.Services;

public static class ReglasPedidoCompra
{
    public static void Transicionar(this PedidoCompra pedido, string accion, string actorId, DateTimeOffset fecha, string? motivo)
    {
        switch (accion)
        {
            case "submit" when pedido.Estado == EstadoPedidoCompra.Borrador && pedido.Detalles.Any():
                pedido.Estado = EstadoPedidoCompra.Enviado; pedido.EnviadoPorId = actorId; pedido.FechaEnvio = fecha; break;
            case "approve" when pedido.Estado == EstadoPedidoCompra.Enviado:
                pedido.Estado = EstadoPedidoCompra.Aprobado; pedido.DecididoPorId = actorId; pedido.FechaDecision = fecha; break;
            case "reject" when pedido.Estado == EstadoPedidoCompra.Enviado && !string.IsNullOrWhiteSpace(motivo):
                pedido.Estado = EstadoPedidoCompra.Rechazado; pedido.DecididoPorId = actorId; pedido.FechaDecision = fecha; pedido.MotivoDecision = motivo; break;
            default: throw new ExcepcionRegla("Invalid purchase transition or missing rejection reason.");
        }
    }

    public static void AsegurarBorrador(PedidoCompra pedido, string mensaje = "Only draft purchases can be edited.")
    {
        if (pedido.Estado != EstadoPedidoCompra.Borrador) throw new ExcepcionRegla(mensaje);
    }

    public static bool EsTransicionPersistidaValida(EstadoPedidoCompra anterior, EstadoPedidoCompra actual) =>
        anterior == actual || anterior == EstadoPedidoCompra.Borrador && actual == EstadoPedidoCompra.Enviado ||
        anterior == EstadoPedidoCompra.Enviado && actual is EstadoPedidoCompra.Aprobado or EstadoPedidoCompra.Rechazado;
}
