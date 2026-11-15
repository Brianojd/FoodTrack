using FoodTrace.API.Application.Interfaces;
using FoodTrace.API.Dtos.Empresas;
using FoodTrace.API.Dtos.Establecimientos;
using FoodTrace.API.Dtos.Productos;
using FoodTrace.API.Dtos.Proveedores;
using FoodTrace.API.Dtos.Comunes;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Application.Services;

public sealed class ServicioCatalogos(FoodTraceDbContext db, SesionEmpresa sesion) : IServicioCatalogos
{
    public async Task<CatalogosDto> ObtenerCatalogosAsync(CancellationToken cancelacion = default) => new()
    {
        Empresa = (await db.Empresas.SingleAsync(x => x.Id == sesion.EmpresaId, cancelacion)).ADto(),
        UnidadesMedida = (await db.UnidadesMedida.AsNoTracking().ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList(),
        Establecimientos = (await db.Establecimientos.AsNoTracking().ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList(),
        Productos = (await db.Productos.AsNoTracking().ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList(),
        Proveedores = (await db.Proveedores.AsNoTracking().ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList(),
        Presentaciones = (await db.PresentacionesProveedor.AsNoTracking().ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList()
    };

    public async Task<EmpresaDto> ActualizarEmpresaAsync(ActualizarEmpresaDto entrada, CancellationToken cancelacion = default)
    {
        var empresa = await db.Empresas.SingleAsync(x => x.Id == sesion.EmpresaId, cancelacion);
        empresa.Nombre = ServicioFlujo.Texto(entrada.Nombre); await db.SaveChangesAsync(cancelacion); return empresa.ADto();
    }

    public async Task<EntidadConNombreDto> CrearEstablecimientoAsync(CrearEstablecimientoDto entrada, CancellationToken cancelacion = default)
    {
        var establecimiento = new Establecimiento { EmpresaId = sesion.EmpresaId, Nombre = ServicioFlujo.Texto(entrada.Nombre) };
        db.Establecimientos.Add(establecimiento); await db.SaveChangesAsync(cancelacion); return establecimiento.ADto();
    }

    public async Task<ProductoDto> CrearProductoAsync(CrearProductoDto entrada, CancellationToken cancelacion = default)
    {
        if (!await db.UnidadesMedida.AnyAsync(x => x.Id == entrada.UnidadBaseId, cancelacion)) throw new ExcepcionRegla("Unknown base unit.");
        var producto = new Producto { EmpresaId = sesion.EmpresaId, Nombre = ServicioFlujo.Texto(entrada.Nombre), UnidadBaseId = entrada.UnidadBaseId };
        db.Productos.Add(producto); await db.SaveChangesAsync(cancelacion); return producto.ADto();
    }

    public async Task<EntidadConNombreDto> CrearProveedorAsync(CrearProveedorDto entrada, CancellationToken cancelacion = default)
    {
        var proveedor = new Proveedor { EmpresaId = sesion.EmpresaId, Nombre = ServicioFlujo.Texto(entrada.Nombre) };
        db.Proveedores.Add(proveedor); await db.SaveChangesAsync(cancelacion); return proveedor.ADto();
    }
}
