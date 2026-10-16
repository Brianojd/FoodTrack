using System.Data;
using FoodTrace.API.Dtos.Proveedores;
using FoodTrace.API.Dtos.Comunes;
using FoodTrace.API.Dtos.Recetas;
using FoodTrace.API.Dtos.Produccion;
using FoodTrace.API.Dtos.Compras;
using FoodTrace.API.Application.Interfaces;
using FoodTrace.API.Infrastructure.Persistence;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Domain.Enums;
using FoodTrace.API.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Application.Services;

public sealed class ExcepcionConflicto(string mensaje) : Exception(mensaje);

public sealed class ServicioFlujo(FoodTraceDbContext db, SesionEmpresa sesion) : IServicioFlujo
{
    public static string Texto(string? valor, int maximo = 500)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Length > maximo)
            throw new ExcepcionRegla($"Text is required and must not exceed {maximo} characters.");
        return valor.Trim();
    }

    private static void ComprobarRevision(EntidadEmpresa entidad, Guid revision)
    {
        if (entidad.Revision != revision) throw new ExcepcionConflicto("This record changed. Refresh before trying again.");
    }

    private async Task<T> BuscarAsync<T>(Guid id, CancellationToken cancelacion) where T : EntidadEmpresa =>
        await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id, cancelacion)
        ?? throw new KeyNotFoundException("Record not found in your company.");

    private async Task<UnidadMedida> ObtenerUnidadAsync(string id, CancellationToken cancelacion) => await db.UnidadesMedida.FindAsync([id], cancelacion)
        ?? throw new ExcepcionRegla("Unknown unit.");

    public async Task<PresentacionProveedorDto> CrearPresentacionAsync(CrearPresentacionProveedorDto entrada, CancellationToken cancelacion = default)
    {
        await BuscarAsync<Proveedor>(entrada.ProveedorId, cancelacion);
        var producto = await BuscarAsync<Producto>(entrada.ProductoId, cancelacion);
        var unidad = await ObtenerUnidadAsync(entrada.UnidadMedidaId, cancelacion);
        var unidadBase = await ObtenerUnidadAsync(producto.UnidadBaseId, cancelacion);
        var presentacion = new PresentacionProveedor
        {
            EmpresaId = sesion.EmpresaId,
            ProveedorId = entrada.ProveedorId,
            ProductoId = entrada.ProductoId,
            Nombre = Texto(entrada.Nombre),
            UnidadMedidaId = unidad.Id,
            Cantidad = Cantidades.Positiva(entrada.Cantidad),
            CantidadBase = Cantidades.Convertir(entrada.Cantidad, unidad.Dimension, unidad.FactorConversion, unidadBase.Dimension, unidadBase.FactorConversion)
        };
        db.PresentacionesProveedor.Add(presentacion); await db.SaveChangesAsync(cancelacion); return presentacion.ADto();
    }

    public Task<VersionRecetaDto> CrearRecetaAsync(CrearRecetaDto entrada, CancellationToken cancelacion = default) =>
        GuardarRecetaAsync(null, entrada, cancelacion);

    public Task<VersionRecetaDto> ActualizarRecetaAsync(Guid id, ActualizarRecetaDto entrada, CancellationToken cancelacion = default) =>
        GuardarRecetaAsync(id, new CrearRecetaDto
        {
            Nombre = entrada.Nombre, ProductoResultadoId = entrada.ProductoResultadoId, Rendimiento = entrada.Rendimiento,
            Instrucciones = entrada.Instrucciones, Ingredientes = entrada.Ingredientes, Revision = entrada.Revision
        }, cancelacion);

    private async Task<VersionRecetaDto> GuardarRecetaAsync(Guid? id, CrearRecetaDto entrada, CancellationToken cancelacion)
    {
        var receta = id is null ? new VersionReceta { EmpresaId = sesion.EmpresaId }
            : await db.VersionesReceta.Include(x => x.Ingredientes).SingleOrDefaultAsync(x => x.Id == id, cancelacion)
                ?? throw new KeyNotFoundException();
        ReglasReceta.AsegurarEditable(receta);
        if (id is not null) ComprobarRevision(receta, entrada.Revision ?? Guid.Empty);
        await BuscarAsync<Producto>(entrada.ProductoResultadoId, cancelacion);
        if (entrada.Ingredientes.Length > 200) throw new ExcepcionRegla("At most 200 ingredients are supported.");
        var ingredientes = new List<IngredienteReceta>();
        foreach (var ingrediente in entrada.Ingredientes)
        {
            var producto = await BuscarAsync<Producto>(ingrediente.ProductoId, cancelacion);
            var unidad = await ObtenerUnidadAsync(ingrediente.UnidadMedidaId, cancelacion); var unidadBase = await ObtenerUnidadAsync(producto.UnidadBaseId, cancelacion);
            Cantidades.Convertir(ingrediente.Cantidad, unidad.Dimension, unidad.FactorConversion, unidadBase.Dimension, unidadBase.FactorConversion);
            ingredientes.Add(new IngredienteReceta
            {
                EmpresaId = sesion.EmpresaId,
                VersionRecetaId = receta.Id,
                ProductoId = ingrediente.ProductoId,
                UnidadMedidaId = unidad.Id,
                Cantidad = ingrediente.Cantidad
            });
        }
        db.IngredientesReceta.RemoveRange(receta.Ingredientes);
        receta.Nombre = Texto(entrada.Nombre); receta.ProductoResultadoId = entrada.ProductoResultadoId;
        receta.Rendimiento = Cantidades.Positiva(entrada.Rendimiento); receta.Instrucciones = Texto(entrada.Instrucciones, 8000);
        receta.Ingredientes = ingredientes;
        db.IngredientesReceta.AddRange(ingredientes);
        if (id is null) db.VersionesReceta.Add(receta);
        await db.SaveChangesAsync(cancelacion); return receta.ADto();
    }

    public async Task<VersionRecetaDto> PublicarRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default)
    {
        var receta = await db.VersionesReceta.Include(x => x.Ingredientes).SingleOrDefaultAsync(x => x.Id == id, cancelacion)
            ?? throw new KeyNotFoundException();
        ComprobarRevision(receta, entrada.Revision); receta.Publicar(); await db.SaveChangesAsync(cancelacion); return receta.ADto();
    }

    public async Task<VersionRecetaDto> CrearVersionRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default)
    {
        var origen = await db.VersionesReceta.Include(x => x.Ingredientes).SingleOrDefaultAsync(x => x.Id == id, cancelacion)
            ?? throw new KeyNotFoundException();
        ComprobarRevision(origen, entrada.Revision);
        if (!origen.Publicada) throw new ExcepcionRegla("Only a published recipe can be versioned.");
        var siguiente = new VersionReceta
        {
            EmpresaId = sesion.EmpresaId,
            FamiliaRecetaId = origen.FamiliaRecetaId,
            NumeroVersion = await db.VersionesReceta.Where(x => x.FamiliaRecetaId == origen.FamiliaRecetaId).MaxAsync(x => x.NumeroVersion, cancelacion) + 1,
            Nombre = origen.Nombre,
            ProductoResultadoId = origen.ProductoResultadoId,
            Rendimiento = origen.Rendimiento,
            Instrucciones = origen.Instrucciones
        };
        siguiente.Ingredientes = origen.Ingredientes.Select(x => new IngredienteReceta
        {
            EmpresaId = sesion.EmpresaId,
            VersionRecetaId = siguiente.Id,
            ProductoId = x.ProductoId,
            Cantidad = x.Cantidad,
            UnidadMedidaId = x.UnidadMedidaId
        }).ToList();
        db.VersionesReceta.Add(siguiente); await db.SaveChangesAsync(cancelacion); return siguiente.ADto();
    }

    public async Task<OrdenProduccionDto> PlanificarProduccionAsync(PlanificarProduccionDto entrada, CancellationToken cancelacion = default)
    {
        await BuscarAsync<Establecimiento>(entrada.EstablecimientoId, cancelacion);
        var receta = await db.VersionesReceta.Include(x => x.Ingredientes).SingleOrDefaultAsync(x => x.Id == entrada.VersionRecetaId, cancelacion)
            ?? throw new KeyNotFoundException();
        if (!receta.Publicada) throw new ExcepcionRegla("Production requires a published recipe version.");
        if (entrada.FechaPlanificada == default) throw new ExcepcionRegla("A production date is required.");
        var resultado = await BuscarAsync<Producto>(receta.ProductoResultadoId, cancelacion);
        var unidadPlanificada = await ObtenerUnidadAsync(entrada.UnidadMedidaId, cancelacion); var unidadResultado = await ObtenerUnidadAsync(resultado.UnidadBaseId, cancelacion);
        var cantidadPlanificada = Cantidades.Convertir(entrada.CantidadPlanificada, unidadPlanificada.Dimension, unidadPlanificada.FactorConversion, unidadResultado.Dimension, unidadResultado.FactorConversion);
        var orden = new OrdenProduccion
        {
            EmpresaId = sesion.EmpresaId,
            EstablecimientoId = entrada.EstablecimientoId,
            VersionRecetaId = receta.Id,
            CantidadPlanificada = cantidadPlanificada,
            FechaPlanificada = entrada.FechaPlanificada,
            CreadoPorId = sesion.UsuarioId,
            FechaCreacion = DateTimeOffset.UtcNow
        };
        var totales = new Dictionary<Guid, (Producto Producto, decimal Cantidad)>();
        foreach (var ingrediente in receta.Ingredientes)
        {
            var producto = await BuscarAsync<Producto>(ingrediente.ProductoId, cancelacion);
            var unidad = await ObtenerUnidadAsync(ingrediente.UnidadMedidaId, cancelacion); var unidadBase = await ObtenerUnidadAsync(producto.UnidadBaseId, cancelacion);
            var cantidad = Cantidades.Necesidad(ingrediente.Cantidad, unidad.FactorConversion, unidadBase.FactorConversion, receta.Rendimiento, cantidadPlanificada);
            totales[producto.Id] = (producto, Cantidades.Positiva(cantidad + (totales.TryGetValue(producto.Id, out var anterior) ? anterior.Cantidad : 0)));
        }
        orden.Necesidades = totales.Values.Select(x => new NecesidadIngrediente
        {
            EmpresaId = sesion.EmpresaId,
            OrdenProduccionId = orden.Id,
            ProductoId = x.Producto.Id,
            UnidadBaseId = x.Producto.UnidadBaseId,
            CantidadNecesaria = x.Cantidad
        }).ToList();
        db.OrdenesProduccion.Add(orden); await db.SaveChangesAsync(cancelacion); return orden.ADto();
    }

    public async Task<PedidoCompraDto> CrearPedidoCompraAsync(CrearPedidoCompraDto entrada, CancellationToken cancelacion = default)
    {
        await BuscarAsync<Proveedor>(entrada.ProveedorId, cancelacion);
        var pedido = new PedidoCompra
        {
            EmpresaId = sesion.EmpresaId,
            ProveedorId = entrada.ProveedorId,
            CreadoPorId = sesion.UsuarioId,
            FechaCreacion = DateTimeOffset.UtcNow
        };
        db.PedidosCompra.Add(pedido); await db.SaveChangesAsync(cancelacion); return pedido.ADto();
    }

    public async Task<PedidoCompraDto> AgregarDetalleCompraAsync(Guid id, AgregarDetalleCompraDto entrada, CancellationToken cancelacion = default)
    {
        // Los bloqueos serializables y las revisiones protegen asignaciones concurrentes.
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelacion);
        var pedido = await ObtenerPedidoAsync(id, cancelacion); ComprobarRevision(pedido, entrada.Revision);
        ReglasPedidoCompra.AsegurarBorrador(pedido);
        var presentacion = await BuscarAsync<PresentacionProveedor>(entrada.PresentacionProveedorId, cancelacion);
        if (presentacion.ProveedorId != pedido.ProveedorId) throw new ExcepcionRegla("The pack must belong to this purchase supplier.");
        if (entrada.Asignaciones.Length == 0 || entrada.Asignaciones.Length > 200)
            throw new ExcepcionRegla("Select between one and 200 saved requirements.");
        if (entrada.Asignaciones.Select(x => x.NecesidadIngredienteId).Distinct().Count() != entrada.Asignaciones.Length)
            throw new ExcepcionRegla("A requirement can appear only once per purchase line.");
        var producto = await BuscarAsync<Producto>(presentacion.ProductoId, cancelacion);
        var detalle = new DetallePedidoCompra
        {
            EmpresaId = sesion.EmpresaId,
            PedidoCompraId = pedido.Id,
            PresentacionProveedorId = presentacion.Id,
            ProductoId = presentacion.ProductoId,
            NombrePresentacion = presentacion.Nombre,
            UnidadBaseId = producto.UnidadBaseId,
            CantidadBasePresentacion = presentacion.CantidadBase,
            CantidadPresentaciones = entrada.CantidadPresentaciones,
            CantidadPedida = Cantidades.CantidadPresentaciones(entrada.CantidadPresentaciones, presentacion.CantidadBase)
        };
        foreach (var asignacion in entrada.Asignaciones)
        {
            var necesidad = await BuscarAsync<NecesidadIngrediente>(asignacion.NecesidadIngredienteId, cancelacion); ComprobarRevision(necesidad, asignacion.Revision);
            if (necesidad.ProductoId != presentacion.ProductoId) throw new ExcepcionRegla("Pack product does not match the saved requirement.");
            var cantidad = Cantidades.Positiva(asignacion.CantidadAsignada);
            if (cantidad > necesidad.CantidadNecesaria - necesidad.CantidadAsignada) throw new ExcepcionConflicto("This gross need has already been allocated. Refresh needs.");
            necesidad.CantidadAsignada += cantidad;
            detalle.Asignaciones.Add(new AsignacionCompra
            {
                EmpresaId = sesion.EmpresaId,
                DetallePedidoCompraId = detalle.Id,
                NecesidadIngredienteId = necesidad.Id,
                CantidadAsignada = cantidad
            });
            detalle.NecesidadBruta += cantidad;
        }
        Cantidades.Positiva(detalle.NecesidadBruta);
        if (detalle.CantidadPedida < detalle.NecesidadBruta) throw new ExcepcionRegla("Whole packages must cover the allocated gross need.");
        pedido.Detalles.Add(detalle); db.DetallesPedidoCompra.Add(detalle); pedido.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(cancelacion); await transaccion.CommitAsync(cancelacion); return pedido.ADto();
    }

    public async Task<PedidoCompraDto> ActualizarPresentacionesAsync(Guid id, Guid detalleId, ActualizarCantidadPresentacionesDto entrada, CancellationToken cancelacion = default)
    {
        var pedido = await ObtenerPedidoAsync(id, cancelacion); ComprobarRevision(pedido, entrada.Revision);
        ReglasPedidoCompra.AsegurarBorrador(pedido);
        var detalle = pedido.Detalles.SingleOrDefault(x => x.Id == detalleId) ?? throw new KeyNotFoundException();
        if (detalle.Asignaciones.All(x => x.Liberada)) throw new ExcepcionRegla("Removed draft lines cannot be edited.");
        var cantidad = Cantidades.CantidadPresentaciones(entrada.CantidadPresentaciones, detalle.CantidadBasePresentacion);
        if (cantidad < detalle.NecesidadBruta) throw new ExcepcionRegla("Packages must cover gross need.");
        detalle.CantidadPresentaciones = entrada.CantidadPresentaciones; detalle.CantidadPedida = cantidad; pedido.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(cancelacion); return pedido.ADto();
    }

    public async Task<PedidoCompraDto> RetirarDetalleCompraAsync(Guid id, Guid detalleId, RevisionDto entrada, CancellationToken cancelacion = default)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelacion);
        var pedido = await ObtenerPedidoAsync(id, cancelacion); ComprobarRevision(pedido, entrada.Revision);
        ReglasPedidoCompra.AsegurarBorrador(pedido, "Only draft lines may be removed.");
        var detalle = pedido.Detalles.SingleOrDefault(x => x.Id == detalleId) ?? throw new KeyNotFoundException();
        if (detalle.Asignaciones.All(x => x.Liberada)) throw new ExcepcionRegla("This line was already removed.");
        foreach (var asignacion in detalle.Asignaciones.Where(x => !x.Liberada))
        {
            var necesidad = await BuscarAsync<NecesidadIngrediente>(asignacion.NecesidadIngredienteId, cancelacion);
            necesidad.CantidadAsignada -= asignacion.CantidadAsignada; asignacion.Liberada = true;
        }
        pedido.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(cancelacion); await transaccion.CommitAsync(cancelacion); return pedido.ADto();
    }

    public Task<PedidoCompraDto> EnviarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) =>
        TransicionarAsync(id, "submit", entrada, cancelacion);

    public Task<PedidoCompraDto> AprobarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) =>
        TransicionarAsync(id, "approve", entrada, cancelacion);

    public Task<PedidoCompraDto> RechazarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) =>
        TransicionarAsync(id, "reject", entrada, cancelacion);

    private async Task<PedidoCompraDto> TransicionarAsync(Guid id, string accion, RevisionDto entrada, CancellationToken cancelacion)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelacion);
        var pedido = await ObtenerPedidoAsync(id, cancelacion); ComprobarRevision(pedido, entrada.Revision);
        pedido.Transicionar(accion, sesion.UsuarioId, DateTimeOffset.UtcNow, entrada.Motivo);
        if (accion == "reject")
            foreach (var asignacion in pedido.Detalles.SelectMany(x => x.Asignaciones).Where(x => !x.Liberada))
            {
                var necesidad = await BuscarAsync<NecesidadIngrediente>(asignacion.NecesidadIngredienteId, cancelacion);
                necesidad.CantidadAsignada -= asignacion.CantidadAsignada; asignacion.Liberada = true;
            }
        await db.SaveChangesAsync(cancelacion); await transaccion.CommitAsync(cancelacion); return pedido.ADto();
    }

    private async Task<PedidoCompra> ObtenerPedidoAsync(Guid id, CancellationToken cancelacion) => await db.PedidosCompra.Include(x => x.Detalles).ThenInclude(x => x.Asignaciones)
        .SingleOrDefaultAsync(x => x.Id == id, cancelacion) ?? throw new KeyNotFoundException();

    public async Task<List<VersionRecetaDto>> ObtenerRecetasAsync(CancellationToken cancelacion = default) =>
        (await db.VersionesReceta.AsNoTracking().Include(x => x.Ingredientes).ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList();
    public async Task<List<OrdenProduccionDto>> ObtenerProduccionAsync(CancellationToken cancelacion = default) =>
        (await db.OrdenesProduccion.AsNoTracking().Include(x => x.Necesidades).ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList();
    public async Task<List<NecesidadIngredienteDto>> ObtenerNecesidadesAsync(CancellationToken cancelacion = default) =>
        (await db.NecesidadesIngredientes.AsNoTracking().ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList();
    public async Task<List<PedidoCompraDto>> ObtenerPedidosCompraAsync(CancellationToken cancelacion = default) =>
        (await db.PedidosCompra.AsNoTracking().Include(x => x.Detalles).ThenInclude(x => x.Asignaciones).ToListAsync(cancelacion)).Select(MapeadorDtos.ADto).ToList();
}
