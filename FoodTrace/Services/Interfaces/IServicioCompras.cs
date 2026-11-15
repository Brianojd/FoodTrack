using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;

namespace FoodTrace.Services.Interfaces;

public interface IServicioCompras
{
    Task<List<PedidoCompraDto>> ObtenerPedidosCompraAsync(CancellationToken cancelacion = default);
    Task<PedidoCompraDto> CrearPedidoCompraAsync(CrearPedidoCompraDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> ActualizarDestinoPedidoCompraAsync(Guid id, CrearPedidoCompraDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> AgregarDetalleCompraAsync(Guid id, AgregarDetalleCompraDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> ActualizarPresentacionesAsync(Guid id, Guid detalleId, ActualizarCantidadPresentacionesDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> RetirarDetalleCompraAsync(Guid id, Guid detalleId, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> EnviarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> GenerarOrdenRecepcionAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<List<OrdenRecepcionDto>> ObtenerOrdenesRecepcionAsync(CancellationToken cancelacion = default);
    Task<OrdenRecepcionDto> ObtenerOrdenRecepcionAsync(Guid id, CancellationToken cancelacion = default);
}
