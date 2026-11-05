using FoodTrace.API.Dtos.Productos;
using FoodTrace.API.Dtos.Empresas;
using FoodTrace.API.Dtos.UnidadesMedida;
using FoodTrace.API.Dtos.Proveedores;
using FoodTrace.API.Dtos.Comunes;
using FoodTrace.API.Dtos.Recetas;
using FoodTrace.API.Dtos.Produccion;
using FoodTrace.API.Dtos.Compras;
using FoodTrace.API.Domain.Entities;

namespace FoodTrace.API.Application.Services;

// Las proyecciones explícitas separan nombres persistidos y contratos JSON históricos.
public static class MapeadorDtos
{
    public static EmpresaDto ADto(this Empresa entidad) => new() { Id = entidad.Id, Nombre = entidad.Nombre };
    public static UnidadMedidaDto ADto(this UnidadMedida entidad) => new() { Id = entidad.Id, Dimension = entidad.Dimension, FactorConversion = entidad.FactorConversion };
    public static EntidadConNombreDto ADto(this Establecimiento entidad) => new() { Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, Nombre = entidad.Nombre };
    public static EntidadConNombreDto ADto(this Proveedor entidad) => new() { Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, Nombre = entidad.Nombre };
    public static ProductoDto ADto(this Producto entidad) => new()
    { Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, Nombre = entidad.Nombre, UnidadBaseId = entidad.UnidadBaseId };
    public static PresentacionProveedorDto ADto(this PresentacionProveedor entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, ProveedorId = entidad.ProveedorId,
        ProductoId = entidad.ProductoId, Nombre = entidad.Nombre, UnidadMedidaId = entidad.UnidadMedidaId, Cantidad = entidad.Cantidad, CantidadBase = entidad.CantidadBase
    };
    public static IngredienteRecetaDto ADto(this IngredienteReceta entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, VersionRecetaId = entidad.VersionRecetaId,
        ProductoId = entidad.ProductoId, UnidadMedidaId = entidad.UnidadMedidaId, Cantidad = entidad.Cantidad
    };
    public static VersionRecetaDto ADto(this VersionReceta entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, FamiliaRecetaId = entidad.FamiliaRecetaId, NumeroVersion = entidad.NumeroVersion, Nombre = entidad.Nombre,
        ProductoResultadoId = entidad.ProductoResultadoId, Rendimiento = entidad.Rendimiento, Instrucciones = entidad.Instrucciones, Publicada = entidad.Publicada,
        CantidadRendimiento = entidad.CantidadRendimiento, UnidadRendimientoId = entidad.UnidadRendimientoId,
        PorcionesRendimiento = entidad.PorcionesRendimiento,
        Ingredientes = entidad.Ingredientes.Select(ADto).ToList()
    };
    public static NecesidadIngredienteDto ADto(this NecesidadIngrediente entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, OrdenProduccionId = entidad.OrdenProduccionId,
        ProductoId = entidad.ProductoId, UnidadBaseId = entidad.UnidadBaseId, CantidadNecesaria = entidad.CantidadNecesaria, CantidadAsignada = entidad.CantidadAsignada
    };
    public static OrdenProduccionDto ADto(this OrdenProduccion entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, EstablecimientoId = entidad.EstablecimientoId,
        VersionRecetaId = entidad.VersionRecetaId, CantidadPlanificada = entidad.CantidadPlanificada, FechaPlanificada = entidad.FechaPlanificada, CreadoPorId = entidad.CreadoPorId,
        FechaCreacion = entidad.FechaCreacion, Necesidades = entidad.Necesidades.Select(ADto).ToList()
    };
    public static AsignacionCompraDto ADto(this AsignacionCompra entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, DetallePedidoCompraId = entidad.DetallePedidoCompraId,
        NecesidadIngredienteId = entidad.NecesidadIngredienteId, CantidadAsignada = entidad.CantidadAsignada, Liberada = entidad.Liberada
    };
    public static DetallePedidoCompraDto ADto(this DetallePedidoCompra entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, PedidoCompraId = entidad.PedidoCompraId,
        PresentacionProveedorId = entidad.PresentacionProveedorId, ProductoId = entidad.ProductoId, NombrePresentacion = entidad.NombrePresentacion, UnidadBaseId = entidad.UnidadBaseId,
        CantidadBasePresentacion = entidad.CantidadBasePresentacion, CantidadPresentaciones = entidad.CantidadPresentaciones, CantidadPedida = entidad.CantidadPedida,
        NecesidadBruta = entidad.NecesidadBruta, Asignaciones = entidad.Asignaciones.Select(ADto).ToList()
    };
    public static PedidoCompraDto ADto(this PedidoCompra entidad) => new()
    {
        Id = entidad.Id, EmpresaId = entidad.EmpresaId, Revision = entidad.Revision, ProveedorId = entidad.ProveedorId, Estado = (int)entidad.Estado,
        CreadoPorId = entidad.CreadoPorId, FechaCreacion = entidad.FechaCreacion, EnviadoPorId = entidad.EnviadoPorId, FechaEnvio = entidad.FechaEnvio,
        DecididoPorId = entidad.DecididoPorId, FechaDecision = entidad.FechaDecision, MotivoDecision = entidad.MotivoDecision, Detalles = entidad.Detalles.Select(ADto).ToList()
    };
}
