using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;

namespace FoodTrace.Services.Interfaces;

public interface IServicioCompras
{
    Task<List<PedidoCompraDto>> ObtenerPedidosCompraAsync(CancellationToken cancelacion = default);
    Task<PedidoCompraDto> CrearPedidoCompraAsync(CrearPedidoCompraDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> AgregarDetalleCompraAsync(Guid id, AgregarDetalleCompraDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> ActualizarPresentacionesAsync(Guid id, Guid detalleId, ActualizarCantidadPresentacionesDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> RetirarDetalleCompraAsync(Guid id, Guid detalleId, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> EnviarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> AprobarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> RechazarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
}
