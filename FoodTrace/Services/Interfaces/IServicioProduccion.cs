using FoodTrace.Dtos.Produccion;

namespace FoodTrace.Services.Interfaces;

public interface IServicioProduccion
{
    Task<List<OrdenProduccionDto>> ObtenerProduccionAsync(CancellationToken cancelacion = default);
    Task<List<NecesidadIngredienteDto>> ObtenerNecesidadesAsync(CancellationToken cancelacion = default);
    Task<OrdenProduccionDto> PlanificarProduccionAsync(PlanificarProduccionDto entrada, CancellationToken cancelacion = default);
}
