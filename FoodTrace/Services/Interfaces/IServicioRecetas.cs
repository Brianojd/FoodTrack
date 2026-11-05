using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Recetas;

namespace FoodTrace.Services.Interfaces;

public interface IServicioRecetas
{
    Task<List<VersionRecetaDto>> ObtenerRecetasAsync(CancellationToken cancelacion = default);
    Task<VersionRecetaDto> CrearRecetaAsync(CrearRecetaDto entrada, CancellationToken cancelacion = default);
    Task<VersionRecetaDto> ActualizarRecetaAsync(Guid id, ActualizarRecetaDto entrada, CancellationToken cancelacion = default);
    Task<VersionRecetaDto> PublicarRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
    Task<VersionRecetaDto> CrearVersionRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default);
}
