using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Empresas;
using FoodTrace.Dtos.Establecimientos;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Proveedores;

namespace FoodTrace.Services.Interfaces;

public interface ICatalogosFoodTrace
{
    Task<CatalogosDto> ObtenerCatalogosAsync(CancellationToken cancelacion = default);
    Task<NombreDto> ActualizarEmpresaAsync(ActualizarEmpresaDto entrada, CancellationToken cancelacion = default);
    Task<NombreDto> CrearEstablecimientoAsync(CrearEstablecimientoDto entrada, CancellationToken cancelacion = default);
    Task<ProductoDto> CrearProductoAsync(CrearProductoDto entrada, CancellationToken cancelacion = default);
    Task<NombreDto> CrearProveedorAsync(CrearProveedorDto entrada, CancellationToken cancelacion = default);
    Task<PresentacionProveedorDto> CrearPresentacionAsync(CrearPresentacionProveedorDto entrada, CancellationToken cancelacion = default);
}
