using FoodTrace.API.Dtos.Proveedores;
using FoodTrace.API.Dtos.Comunes;
using FoodTrace.API.Dtos.Recetas;
using FoodTrace.API.Dtos.Produccion;
using FoodTrace.API.Dtos.Compras;

namespace FoodTrace.API.Application.Interfaces;

public interface IServicioFlujo
{
    Task<PresentacionProveedorDto> CrearPresentacionAsync(CrearPresentacionProveedorDto entrada, CancellationToken cancelacion = default);
    Task<List<VersionRecetaDto>> ObtenerRecetasAsync(CancellationToken cancelacion = default);
    Task<VersionRecetaDto> CrearRecetaAsync(CrearRecetaDto entrada, CancellationToken cancelacion = default);
    Task<VersionRecetaDto> ActualizarRecetaAsync(Guid id, ActualizarRecetaDto entrada, CancellationToken cancelacion = default);
    Task<VersionRecetaDto> PublicarRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<VersionRecetaDto> CrearVersionRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<List<OrdenProduccionDto>> ObtenerProduccionAsync(CancellationToken cancelacion = default);
    Task<List<NecesidadIngredienteDto>> ObtenerNecesidadesAsync(CancellationToken cancelacion = default);
    Task<OrdenProduccionDto> PlanificarProduccionAsync(PlanificarProduccionDto entrada, CancellationToken cancelacion = default);
    Task<List<PedidoCompraDto>> ObtenerPedidosCompraAsync(CancellationToken cancelacion = default);
    Task<PedidoCompraDto> CrearPedidoCompraAsync(CrearPedidoCompraDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> AgregarDetalleCompraAsync(Guid id, AgregarDetalleCompraDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> ActualizarPresentacionesAsync(Guid id, Guid detalleId, ActualizarCantidadPresentacionesDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> RetirarDetalleCompraAsync(Guid id, Guid detalleId, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> EnviarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> AprobarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<PedidoCompraDto> RechazarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
}
