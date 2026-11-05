using FoodTrace.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Persistence.Configurations;

internal static class ConfiguracionEsquemaEspanol
{
    public static void Configurar(ModelBuilder modelo)
    {
        Tabla<Empresa>(modelo, "Empresas", (nameof(Empresa.Nombre), "Nombre"));
        Tabla<Establecimiento>(modelo, "Establecimientos", (nameof(Establecimiento.Nombre), "Nombre"));
        Tabla<Producto>(modelo, "Productos", (nameof(Producto.Nombre), "Nombre"), (nameof(Producto.UnidadBaseId), "UnidadBaseId"));
        Tabla<UnidadMedida>(modelo, "UnidadesMedida", (nameof(UnidadMedida.FactorConversion), "FactorConversion"));
        Tabla<Proveedor>(modelo, "Proveedores", (nameof(Proveedor.Nombre), "Nombre"));
        Tabla<PresentacionProveedor>(modelo, "PresentacionesProveedor",
            (nameof(PresentacionProveedor.ProveedorId), "ProveedorId"), (nameof(PresentacionProveedor.ProductoId), "ProductoId"),
            (nameof(PresentacionProveedor.Nombre), "Nombre"), (nameof(PresentacionProveedor.UnidadMedidaId), "UnidadMedidaId"),
            (nameof(PresentacionProveedor.Cantidad), "Cantidad"), (nameof(PresentacionProveedor.CantidadBase), "CantidadBase"));
        Tabla<VersionReceta>(modelo, "VersionesReceta",
            (nameof(VersionReceta.FamiliaRecetaId), "FamiliaRecetaId"), (nameof(VersionReceta.NumeroVersion), "NumeroVersion"),
            (nameof(VersionReceta.Nombre), "Nombre"), (nameof(VersionReceta.ProductoResultadoId), "ProductoResultadoId"),
            (nameof(VersionReceta.Rendimiento), "Rendimiento"), (nameof(VersionReceta.Instrucciones), "Instrucciones"),
            (nameof(VersionReceta.Publicada), "Publicada"),
            (nameof(VersionReceta.CantidadRendimiento), "CantidadRendimiento"),
            (nameof(VersionReceta.UnidadRendimientoId), "UnidadRendimientoId"),
            (nameof(VersionReceta.PorcionesRendimiento), "PorcionesRendimiento"));
        Tabla<IngredienteReceta>(modelo, "IngredientesReceta",
            (nameof(IngredienteReceta.VersionRecetaId), "VersionRecetaId"), (nameof(IngredienteReceta.ProductoId), "ProductoId"),
            (nameof(IngredienteReceta.UnidadMedidaId), "UnidadMedidaId"), (nameof(IngredienteReceta.Cantidad), "Cantidad"));
        Tabla<OrdenProduccion>(modelo, "OrdenesProduccion",
            (nameof(OrdenProduccion.EstablecimientoId), "EstablecimientoId"), (nameof(OrdenProduccion.VersionRecetaId), "VersionRecetaId"),
            (nameof(OrdenProduccion.CantidadPlanificada), "CantidadPlanificada"), (nameof(OrdenProduccion.FechaPlanificada), "FechaPlanificada"),
            (nameof(OrdenProduccion.CreadoPorId), "CreadoPorId"), (nameof(OrdenProduccion.FechaCreacion), "FechaCreacion"));
        Tabla<NecesidadIngrediente>(modelo, "NecesidadesIngredientes",
            (nameof(NecesidadIngrediente.OrdenProduccionId), "OrdenProduccionId"), (nameof(NecesidadIngrediente.ProductoId), "ProductoId"),
            (nameof(NecesidadIngrediente.UnidadBaseId), "UnidadBaseId"), (nameof(NecesidadIngrediente.CantidadNecesaria), "CantidadNecesaria"),
            (nameof(NecesidadIngrediente.CantidadAsignada), "CantidadAsignada"));
        Tabla<PedidoCompra>(modelo, "PedidosCompra",
            (nameof(PedidoCompra.ProveedorId), "ProveedorId"), (nameof(PedidoCompra.Estado), "Estado"),
            (nameof(PedidoCompra.CreadoPorId), "CreadoPorId"), (nameof(PedidoCompra.FechaCreacion), "FechaCreacion"),
            (nameof(PedidoCompra.EnviadoPorId), "EnviadoPorId"), (nameof(PedidoCompra.FechaEnvio), "FechaEnvio"),
            (nameof(PedidoCompra.DecididoPorId), "DecididoPorId"), (nameof(PedidoCompra.FechaDecision), "FechaDecision"),
            (nameof(PedidoCompra.MotivoDecision), "MotivoDecision"));
        Tabla<DetallePedidoCompra>(modelo, "DetallesPedidoCompra",
            (nameof(DetallePedidoCompra.PedidoCompraId), "PedidoCompraId"), (nameof(DetallePedidoCompra.PresentacionProveedorId), "PresentacionProveedorId"),
            (nameof(DetallePedidoCompra.ProductoId), "ProductoId"), (nameof(DetallePedidoCompra.NombrePresentacion), "NombrePresentacion"),
            (nameof(DetallePedidoCompra.UnidadBaseId), "UnidadBaseId"), (nameof(DetallePedidoCompra.CantidadBasePresentacion), "CantidadBasePresentacion"),
            (nameof(DetallePedidoCompra.CantidadPresentaciones), "CantidadPresentaciones"), (nameof(DetallePedidoCompra.CantidadPedida), "CantidadPedida"),
            (nameof(DetallePedidoCompra.NecesidadBruta), "NecesidadBruta"));
        Tabla<AsignacionCompra>(modelo, "AsignacionesCompra",
            (nameof(AsignacionCompra.DetallePedidoCompraId), "DetallePedidoCompraId"), (nameof(AsignacionCompra.NecesidadIngredienteId), "NecesidadIngredienteId"),
            (nameof(AsignacionCompra.CantidadAsignada), "CantidadAsignada"), (nameof(AsignacionCompra.Liberada), "Liberada"));
        foreach (var entidad in modelo.Model.GetEntityTypes().Where(x => typeof(EntidadEmpresa).IsAssignableFrom(x.ClrType)))
            entidad.FindProperty(nameof(EntidadEmpresa.EmpresaId))!.SetColumnName("EmpresaId");
    }

    private static void Tabla<T>(ModelBuilder modelo, string nombre, params (string Propiedad, string Columna)[] columnas)
        where T : class
    {
        var entidad = modelo.Entity<T>().ToTable(nombre);
        foreach (var (propiedad, columna) in columnas) entidad.Property(propiedad).HasColumnName(columna);
    }
}
