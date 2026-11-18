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
            (nameof(PedidoCompra.ProveedorId), "ProveedorId"), (nameof(PedidoCompra.EstablecimientoId), "EstablecimientoId"), (nameof(PedidoCompra.Estado), "Estado"),
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
        Tabla<OrdenRecepcion>(modelo, "OrdenesRecepcion",
            (nameof(OrdenRecepcion.PedidoCompraId), "PedidoCompraId"), (nameof(OrdenRecepcion.ProveedorId), "ProveedorId"),
            (nameof(OrdenRecepcion.NombreProveedor), "NombreProveedor"), (nameof(OrdenRecepcion.EstablecimientoId), "EstablecimientoId"),
            (nameof(OrdenRecepcion.NombreEstablecimiento), "NombreEstablecimiento"), (nameof(OrdenRecepcion.FechaCreacion), "FechaCreacion"),
            (nameof(OrdenRecepcion.CreadoPorId), "CreadoPorId"), (nameof(OrdenRecepcion.Estado), "Estado"));
        Tabla<DetalleOrdenRecepcion>(modelo, "DetallesOrdenRecepcion",
            (nameof(DetalleOrdenRecepcion.OrdenRecepcionId), "OrdenRecepcionId"), (nameof(DetalleOrdenRecepcion.ProductoId), "ProductoId"),
            (nameof(DetalleOrdenRecepcion.NombreProducto), "NombreProducto"), (nameof(DetalleOrdenRecepcion.PresentacionProveedorId), "PresentacionProveedorId"),
            (nameof(DetalleOrdenRecepcion.NombrePresentacion), "NombrePresentacion"), (nameof(DetalleOrdenRecepcion.CantidadPresentaciones), "CantidadPresentaciones"),
            (nameof(DetalleOrdenRecepcion.CantidadBasePresentacion), "CantidadBasePresentacion"), (nameof(DetalleOrdenRecepcion.CantidadEsperada), "CantidadEsperada"),
            (nameof(DetalleOrdenRecepcion.UnidadBaseId), "UnidadBaseId"));
        Tabla<Recepcion>(modelo, "Recepciones",
            (nameof(Recepcion.OrdenRecepcionId), "OrdenRecepcionId"), (nameof(Recepcion.UsuarioReceptorId), "UsuarioReceptorId"),
            (nameof(Recepcion.FechaRecepcion), "FechaRecepcion"), (nameof(Recepcion.Observacion), "Observacion"));
        Tabla<DetalleRecepcion>(modelo, "DetallesRecepcion",
            (nameof(DetalleRecepcion.RecepcionId), "RecepcionId"), (nameof(DetalleRecepcion.DetalleOrdenRecepcionId), "DetalleOrdenRecepcionId"),
            (nameof(DetalleRecepcion.OrdenRecepcionId), "OrdenRecepcionId"), (nameof(DetalleRecepcion.ProductoId), "ProductoId"),
            (nameof(DetalleRecepcion.NombreProducto), "NombreProducto"), (nameof(DetalleRecepcion.PresentacionProveedorId), "PresentacionProveedorId"),
            (nameof(DetalleRecepcion.NombrePresentacion), "NombrePresentacion"), (nameof(DetalleRecepcion.CantidadPresentacionesEsperadas), "CantidadPresentacionesEsperadas"),
            (nameof(DetalleRecepcion.CantidadPresentacionesRecibidas), "CantidadPresentacionesRecibidas"), (nameof(DetalleRecepcion.CantidadPresentacionesFaltantes), "CantidadPresentacionesFaltantes"),
            (nameof(DetalleRecepcion.CantidadBasePresentacion), "CantidadBasePresentacion"), (nameof(DetalleRecepcion.CantidadEsperada), "CantidadEsperada"),
            (nameof(DetalleRecepcion.CantidadRecibida), "CantidadRecibida"), (nameof(DetalleRecepcion.CantidadFaltante), "CantidadFaltante"),
            (nameof(DetalleRecepcion.UnidadBaseId), "UnidadBaseId"));
        Tabla<IngresoInventario>(modelo, "IngresosInventario",
            (nameof(IngresoInventario.RecepcionId), "RecepcionId"), (nameof(IngresoInventario.DetalleRecepcionId), "DetalleRecepcionId"),
            (nameof(IngresoInventario.OrdenRecepcionId), "OrdenRecepcionId"), (nameof(IngresoInventario.ProductoId), "ProductoId"),
            (nameof(IngresoInventario.EstablecimientoId), "EstablecimientoId"), (nameof(IngresoInventario.Cantidad), "Cantidad"),
            (nameof(IngresoInventario.UnidadBaseId), "UnidadBaseId"), (nameof(IngresoInventario.FechaIngreso), "FechaIngreso"));
        Tabla<Recepcion>(modelo, "Recepciones",
            (nameof(Recepcion.OrdenRecepcionId), "OrdenRecepcionId"), (nameof(Recepcion.UsuarioReceptorId), "UsuarioReceptorId"),
            (nameof(Recepcion.FechaRecepcion), "FechaRecepcion"), (nameof(Recepcion.Observacion), "Observacion"));
        Tabla<DetalleRecepcion>(modelo, "DetallesRecepcion",
            (nameof(DetalleRecepcion.RecepcionId), "RecepcionId"), (nameof(DetalleRecepcion.DetalleOrdenRecepcionId), "DetalleOrdenRecepcionId"),
            (nameof(DetalleRecepcion.OrdenRecepcionId), "OrdenRecepcionId"), (nameof(DetalleRecepcion.ProductoId), "ProductoId"),
            (nameof(DetalleRecepcion.NombreProducto), "NombreProducto"), (nameof(DetalleRecepcion.PresentacionProveedorId), "PresentacionProveedorId"),
            (nameof(DetalleRecepcion.NombrePresentacion), "NombrePresentacion"), (nameof(DetalleRecepcion.CantidadPresentacionesEsperadas), "CantidadPresentacionesEsperadas"),
            (nameof(DetalleRecepcion.CantidadPresentacionesRecibidas), "CantidadPresentacionesRecibidas"), (nameof(DetalleRecepcion.CantidadPresentacionesFaltantes), "CantidadPresentacionesFaltantes"),
            (nameof(DetalleRecepcion.CantidadBasePresentacion), "CantidadBasePresentacion"), (nameof(DetalleRecepcion.CantidadEsperada), "CantidadEsperada"),
            (nameof(DetalleRecepcion.CantidadRecibida), "CantidadRecibida"), (nameof(DetalleRecepcion.CantidadFaltante), "CantidadFaltante"),
            (nameof(DetalleRecepcion.UnidadBaseId), "UnidadBaseId"));
        Tabla<IngresoInventario>(modelo, "IngresosInventario",
            (nameof(IngresoInventario.RecepcionId), "RecepcionId"), (nameof(IngresoInventario.DetalleRecepcionId), "DetalleRecepcionId"),
            (nameof(IngresoInventario.OrdenRecepcionId), "OrdenRecepcionId"), (nameof(IngresoInventario.ProductoId), "ProductoId"),
            (nameof(IngresoInventario.EstablecimientoId), "EstablecimientoId"), (nameof(IngresoInventario.Cantidad), "Cantidad"),
            (nameof(IngresoInventario.UnidadBaseId), "UnidadBaseId"), (nameof(IngresoInventario.FechaIngreso), "FechaIngreso"));
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
