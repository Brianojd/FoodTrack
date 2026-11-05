using FoodTrace.Models.Administracion;
using FoodTrace.Models.Compras;
using FoodTrace.Models.Produccion;
using FoodTrace.Models.Recetas;

namespace FoodTrace.Models.Validacion;

// Explicit wire/MVC-to-model identities. No reflection, DTO mutation or form generation.
public static class MapasValidacionServidor
{
    public static MapaCamposServidor Nombre(FormularioNombre m) => new MapaCamposServidor(m).Campo(m, nameof(m.Nombre), "Name", "Nombre");
    public static MapaCamposServidor Producto(FormularioProducto m) => new MapaCamposServidor(m)
        .Campo(m, nameof(m.Nombre), "Name", "Nombre").Campo(m, nameof(m.UnidadBaseId), "BaseUnitId", "UnidadBaseId");
    public static MapaCamposServidor Presentacion(FormularioPresentacion m) => new MapaCamposServidor(m)
        .Campo(m, nameof(m.Nombre), "Name", "Nombre").Campo(m, nameof(m.ProveedorId), "SupplierId", "ProveedorId")
        .Campo(m, nameof(m.ProductoId), "ProductId", "ProductoId").Campo(m, nameof(m.UnidadMedidaId), "UnitId", "UnidadMedidaId")
        .Campo(m, nameof(m.Cantidad), "Quantity", "Cantidad");
    public static MapaCamposServidor Usuario(FormularioUsuario m) => Roles(new MapaCamposServidor(m)
        .Campo(m, nameof(m.Correo), "Email", "Correo").Campo(m, nameof(m.Contrasena), "Password", "Contrasena"), m, m.Roles);
    public static MapaCamposServidor Membresia(FormularioMembresia m) => Roles(new MapaCamposServidor(m)
        .Campo(m, nameof(m.Habilitado), "Enabled", "Habilitado"), m, m.Roles);
    private static MapaCamposServidor Roles(MapaCamposServidor mapa, object m, string[] roles)
    {
        mapa.Campo(m, "Roles", "Roles");
        for (var i = 0; i < roles.Length; i++) mapa.Campo(m, "Roles", $"Roles[{i}]");
        return mapa;
    }
    public static MapaCamposServidor Receta(ModeloVersionReceta m)
    {
        var mapa = new MapaCamposServidor(m).Campo(m, nameof(m.Nombre), "Name", "Nombre")
            .Campo(m, nameof(m.ProductoResultadoId), "OutputProductId", "ProductoResultadoId")
            .Campo(m, nameof(m.Rendimiento), "Yield", "Rendimiento").Campo(m, nameof(m.Instrucciones), "Instructions", "Instrucciones")
            .Campo(m, nameof(m.UnidadRendimientoId), "YieldUnitId", "UnidadRendimientoId")
            .Campo(m, nameof(m.PorcionesRendimiento), "YieldPortions", "PorcionesRendimiento")
            .Campo(m, nameof(m.Ingredientes), "Ingredients", "Ingredientes");
        for (var i = 0; i < m.Ingredientes.Count; i++)
        {
            var fila = m.Ingredientes[i];
            foreach (var raiz in new[] { "Ingredients", "Ingredientes" })
            {
                var ruta = $"{raiz}[{i}]";
                mapa.Campo(m, nameof(m.Ingredientes), ruta)
                    .Campo(fila, nameof(fila.ProductoId), ruta + ".ProductId", ruta + ".ProductoId")
                    .Campo(fila, nameof(fila.UnidadMedidaId), ruta + ".UnitId", ruta + ".UnidadMedidaId")
                    .Campo(fila, nameof(fila.Cantidad), ruta + ".Quantity", ruta + ".Cantidad");
            }
        }
        return mapa;
    }
    public static MapaCamposServidor Planificacion(ModeloPlanificacion m) => new MapaCamposServidor(m)
        .Campo(m, nameof(m.EstablecimientoId), "EstablishmentId", "EstablecimientoId")
        .Campo(m, nameof(m.VersionRecetaId), "RecipeVersionId", "VersionRecetaId").Campo(m, nameof(m.Cantidad), "Quantity", "CantidadPlanificada", "Cantidad")
        .Campo(m, nameof(m.UnidadMedidaId), "UnitId", "UnidadMedidaId").Campo(m, nameof(m.PorPorciones), "ByPortions", "PorPorciones")
        .Campo(m, nameof(m.Fecha), "Date", "FechaPlanificada", "Fecha");
    public static MapaCamposServidor Pedido(ModeloPedidoCompra m) => new MapaCamposServidor(m).Campo(m, nameof(m.ProveedorId), "SupplierId", "ProveedorId");
    public static MapaCamposServidor Detalle(ModeloDetallePedido m)
    {
        var mapa = new MapaCamposServidor(m).Campo(m, nameof(m.PresentacionId), "SupplierPackId", "PackId", "PresentacionProveedorId", "PresentacionId")
            .Campo(m, nameof(m.CantidadPresentaciones), "PackCount", "CantidadPresentaciones")
            .Campo(m, nameof(m.Asignaciones), "Allocations", "Asignaciones");
        // Exactly the Values order used by ModeloDetallePedido.ParaEnviar, NOT the filtered needs view.
        var enviadas = m.Asignaciones.Values.ToArray();
        for (var i = 0; i < enviadas.Length; i++)
            foreach (var raiz in new[] { "Allocations", "Asignaciones" })
            {
                var ruta = $"{raiz}[{i}]";
                var fila = enviadas[i];
                mapa.Campo(m, nameof(m.Asignaciones), ruta)
                    .Campo(fila, nameof(fila.Cantidad), ruta + ".Quantity", ruta + ".Amount", ruta + ".CantidadAsignada", ruta + ".Cantidad")
                    .Campo(fila, nameof(fila.NecesidadId), ruta + ".RequirementId", ruta + ".NecesidadIngredienteId", ruta + ".NecesidadId");
                // Revision is readonly: deliberately unmatched, so it stays in the general summary.
            }
        return mapa;
    }
    public static MapaCamposServidor Cantidad(ModeloCantidadPresentaciones m) => new MapaCamposServidor(m).Campo(m, nameof(m.Cantidad), "PackCount", "CantidadPresentaciones", "Cantidad");
    public static MapaCamposServidor Rechazo(ModeloRechazoCompra m) => new MapaCamposServidor(m).Campo(m, nameof(m.Motivo), "Reason", "Motivo");
}
