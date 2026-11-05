using FoodTrace.API.Dtos.Productos;
using FoodTrace.API.Dtos.Empresas;
using FoodTrace.API.Dtos.Establecimientos;
using FoodTrace.API.Dtos.Proveedores;
using FoodTrace.API.Dtos.Comunes;

namespace FoodTrace.API.Application.Interfaces;

public interface IServicioCatalogos
{
    Task<CatalogosDto> ObtenerCatalogosAsync(CancellationToken cancelacion = default);
    Task<EmpresaDto> ActualizarEmpresaAsync(ActualizarEmpresaDto entrada, CancellationToken cancelacion = default);
    Task<EntidadConNombreDto> CrearEstablecimientoAsync(CrearEstablecimientoDto entrada, CancellationToken cancelacion = default);
    Task<ProductoDto> CrearProductoAsync(CrearProductoDto entrada, CancellationToken cancelacion = default);
    Task<EntidadConNombreDto> CrearProveedorAsync(CrearProveedorDto entrada, CancellationToken cancelacion = default);
}
