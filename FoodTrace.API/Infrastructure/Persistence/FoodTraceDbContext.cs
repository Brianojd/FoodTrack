using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Domain.Enums;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Persistence;

public sealed class FoodTraceDbContext(DbContextOptions<FoodTraceDbContext> opciones, SesionEmpresa sesion)
    : IdentityDbContext<AppUser>(opciones)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Establecimiento> Establecimientos => Set<Establecimiento>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<PresentacionProveedor> PresentacionesProveedor => Set<PresentacionProveedor>();
    public DbSet<VersionReceta> VersionesReceta => Set<VersionReceta>();
    public DbSet<IngredienteReceta> IngredientesReceta => Set<IngredienteReceta>();
    public DbSet<OrdenProduccion> OrdenesProduccion => Set<OrdenProduccion>();
    public DbSet<NecesidadIngrediente> NecesidadesIngredientes => Set<NecesidadIngrediente>();
    public DbSet<PedidoCompra> PedidosCompra => Set<PedidoCompra>();
    public DbSet<DetallePedidoCompra> DetallesPedidoCompra => Set<DetallePedidoCompra>();
    public DbSet<AsignacionCompra> AsignacionesCompra => Set<AsignacionCompra>();
    public DbSet<OrdenRecepcion> OrdenesRecepcion => Set<OrdenRecepcion>();
    public DbSet<DetalleOrdenRecepcion> DetallesOrdenRecepcion => Set<DetalleOrdenRecepcion>();
    public DbSet<Recepcion> Recepciones => Set<Recepcion>();
    public DbSet<DetalleRecepcion> DetallesRecepcion => Set<DetalleRecepcion>();
    public DbSet<IngresoInventario> IngresosInventario => Set<IngresoInventario>();

    public Guid EmpresaActualId => sesion.EmpresaId;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        ConfiguracionModelo.Configurar(b, this);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidarAsync().GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await ValidarAsync();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task ValidarAsync()
    {
        ChangeTracker.DetectChanges();
        var changed = ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        foreach (var entry in changed)
        {
            if (entry.Entity is AppUser && entry.State != EntityState.Added && entry.Property(nameof(AppUser.EmpresaId)).IsModified)
                throw new ExcepcionRegla("User company membership is immutable.");
            if (entry.Entity is Empresa company && entry.State != EntityState.Added && company.Id != sesion.EmpresaId)
                throw new ExcepcionRegla("Only your company metadata may be edited.");
            if (entry.Entity is EntidadEmpresa row)
            {
                if (row.EmpresaId != sesion.EmpresaId || row.EmpresaId == Guid.Empty)
                    throw new ExcepcionRegla("The row does not belong to the authenticated company.");
                if (entry.State != EntityState.Added && (Guid)entry.OriginalValues[nameof(EntidadEmpresa.EmpresaId)]! != row.EmpresaId)
                    throw new ExcepcionRegla("Company membership is immutable.");
                if (entry.State == EntityState.Modified) row.Revision = Guid.NewGuid();
            }
            if (entry.Entity is UnidadMedida && entry.State != EntityState.Added)
                throw new ExcepcionRegla("Standard units and conversion factors are immutable.");
            if (entry.Entity is Producto && entry.State != EntityState.Added && entry.Property(nameof(Producto.UnidadBaseId)).IsModified)
                throw new ExcepcionRegla("Product base units are immutable. Create a new product instead.");
            if (entry.Entity is PresentacionProveedor && entry.State != EntityState.Added)
                throw new ExcepcionRegla("Supplier packs are immutable. Create a new pack instead.");
            if (entry.Entity is VersionReceta recipe && entry.State != EntityState.Added)
            {
                var prior = await VersionesReceta.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == recipe.Id);
                if (prior.Publicada) throw new ExcepcionRegla("Published recipes are immutable. Create a new draft version.");
            }
            if (entry.Entity is VersionReceta version && entry.State != EntityState.Deleted)
            {
                Cantidades.Positiva(version.Rendimiento);
                if (version.PorcionesRendimiento is { } porciones) Cantidades.Positiva(porciones);
                if ((version.CantidadRendimiento is null) != (version.UnidadRendimientoId is null))
                    throw new ExcepcionRegla("La cantidad y unidad de rendimiento deben informarse juntas.");
                if (version.CantidadRendimiento is { } cantidad)
                {
                    var producto = await Productos.SingleAsync(x => x.Id == version.ProductoResultadoId);
                    var unidad = await UnidadesMedida.SingleAsync(x => x.Id == version.UnidadRendimientoId);
                    var unidadBase = await UnidadesMedida.SingleAsync(x => x.Id == producto.UnidadBaseId);
                    var normalizado = Cantidades.Positiva(Cantidades.ConvertirExacto(cantidad, unidad.Dimension,
                        unidad.FactorConversion, unidadBase.Dimension, unidadBase.FactorConversion));
                    if (normalizado != version.Rendimiento) throw new ExcepcionRegla("El rendimiento no coincide con su cantidad y unidad.");
                }
            }
            if (entry.Entity is VersionReceta publishing && publishing.Publicada)
            {
                publishing.Publicar();
                foreach (var item in publishing.Ingredientes)
                {
                    var product = await Productos.SingleAsync(x => x.Id == item.ProductoId);
                    var unit = await UnidadesMedida.SingleAsync(x => x.Id == item.UnidadMedidaId);
                    var baseUnit = await UnidadesMedida.SingleAsync(x => x.Id == product.UnidadBaseId);
                    Cantidades.Convertir(item.Cantidad, unit.Dimension, unit.FactorConversion, baseUnit.Dimension, baseUnit.FactorConversion);
                }
            }
            if (entry.Entity is IngredienteReceta ingredient)
            {
                var parentId = entry.State == EntityState.Added ? ingredient.VersionRecetaId
                    : (Guid)entry.OriginalValues[nameof(IngredienteReceta.VersionRecetaId)]!;
                if (await VersionesReceta.IgnoreQueryFilters().AnyAsync(x => x.Id == parentId && x.Publicada))
                    throw new ExcepcionRegla("Published recipe ingredients are immutable.");
                if (parentId != ingredient.VersionRecetaId)
                    throw new ExcepcionRegla("Ingredient ownership is immutable.");
            }
            if (entry.Entity is OrdenProduccion && entry.State != EntityState.Added)
                throw new ExcepcionRegla("Production snapshots are immutable.");
            if (entry.Entity is NecesidadIngrediente addedNeed && entry.State == EntityState.Added &&
                !changed.Any(x => x.Entity is OrdenProduccion parent && parent.Id == addedNeed.OrdenProduccionId && x.State == EntityState.Added))
                throw new ExcepcionRegla("Requirements must be created atomically with a production snapshot.");
            if (entry.Entity is NecesidadIngrediente requirement && entry.State != EntityState.Added)
            {
                if (entry.State == EntityState.Deleted || entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not nameof(NecesidadIngrediente.CantidadAsignada) and not nameof(EntidadEmpresa.Revision)))
                    throw new ExcepcionRegla("Requirement snapshots are immutable.");
                if (requirement.CantidadAsignada < 0 || requirement.CantidadAsignada > requirement.CantidadNecesaria)
                    throw new ExcepcionRegla("Allocation exceeds saved gross need.");
            }
            if (entry.Entity is PedidoCompra purchase && entry.State != EntityState.Added)
            {
                var prior = await PedidosCompra.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == purchase.Id);
                if (prior.Estado is EstadoPedidoCompra.Aprobado or EstadoPedidoCompra.Rechazado)
                    throw new ExcepcionRegla("Decided purchases are immutable.");
                var valid = ReglasPedidoCompra.EsTransicionPersistidaValida(prior.Estado, purchase.Estado);
                if (!valid || entry.State == EntityState.Deleted) throw new ExcepcionRegla("Invalid persisted purchase transition.");
                if (purchase.CreadoPorId != prior.CreadoPorId || purchase.FechaCreacion != prior.FechaCreacion || purchase.ProveedorId != prior.ProveedorId)
                    throw new ExcepcionRegla("Purchase ownership and creation audit are immutable.");
                if (purchase.EstablecimientoId != prior.EstablecimientoId && purchase.EstablecimientoId is { } nuevoDestino)
                    await BuscarDestinoValidoAsync(nuevoDestino, purchase.EmpresaId);
                if (purchase.EstablecimientoId != prior.EstablecimientoId &&
                    await OrdenesRecepcion.IgnoreQueryFilters().AnyAsync(x => x.EmpresaId == purchase.EmpresaId && x.PedidoCompraId == purchase.Id))
                    throw new ExcepcionRegla("The destination cannot change after a receipt order exists.");
            }
            if (entry.Entity is DetallePedidoCompra line)
            {
                if (await PedidosCompra.IgnoreQueryFilters().AnyAsync(x => x.Id == line.PedidoCompraId && x.Estado != EstadoPedidoCompra.Borrador))
                    throw new ExcepcionRegla("Submitted purchase lines are immutable.");
                if (entry.State != EntityState.Added && entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not nameof(DetallePedidoCompra.CantidadPresentaciones) and not nameof(DetallePedidoCompra.CantidadPedida) and not nameof(EntidadEmpresa.Revision)))
                    throw new ExcepcionRegla("Only whole package count may change on an existing draft line.");
                if (line.CantidadPedida != Cantidades.CantidadPresentaciones(line.CantidadPresentaciones, line.CantidadBasePresentacion) || line.CantidadPedida < line.NecesidadBruta)
                    throw new ExcepcionRegla("Invalid whole-package snapshot.");
                if (entry.State == EntityState.Added)
                {
                if (line.Asignaciones.Count == 0 ? line.NecesidadBruta != 0 : line.Asignaciones.Sum(x => x.CantidadAsignada) != line.NecesidadBruta)
                    throw new ExcepcionRegla("Line gross need must equal its saved allocations.");
                    var pack = await PresentacionesProveedor.SingleAsync(x => x.Id == line.PresentacionProveedorId);
                    var order = ChangeTracker.Entries<PedidoCompra>().Select(x => x.Entity).SingleOrDefault(x => x.Id == line.PedidoCompraId)
                        ?? await PedidosCompra.SingleAsync(x => x.Id == line.PedidoCompraId);
                    if (pack.ProveedorId != order.ProveedorId || pack.ProductoId != line.ProductoId || pack.CantidadBase != line.CantidadBasePresentacion || pack.Nombre != line.NombrePresentacion)
                        throw new ExcepcionRegla("Purchase packaging snapshot must match the selected supplier pack.");
                }
            }
            if (entry.Entity is OrdenRecepcion && entry.State != EntityState.Added)
            {
                if (entry.State == EntityState.Deleted || entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not nameof(OrdenRecepcion.Estado) and not nameof(EntidadEmpresa.Revision)))
                    throw new ExcepcionRegla("Receipt order snapshots are immutable; only its closing status may change.");
            }
            if (entry.Entity is OrdenRecepcion orden && entry.State == EntityState.Added)
                await ValidarNuevaOrdenAsync(orden);
            if (entry.Entity is DetalleOrdenRecepcion && entry.State != EntityState.Added)
                throw new ExcepcionRegla("Receipt order detail snapshots are immutable.");
            if (entry.Entity is DetalleOrdenRecepcion detalle && entry.State == EntityState.Added)
                await ValidarNuevoDetalleOrdenAsync(detalle);
            if (entry.Entity is Recepcion && entry.State != EntityState.Added)
                throw new ExcepcionRegla("Saved receipts are immutable.");
            if (entry.Entity is DetalleRecepcion && entry.State != EntityState.Added)
                throw new ExcepcionRegla("Saved receipt details are immutable.");
            if (entry.Entity is IngresoInventario && entry.State != EntityState.Added)
                throw new ExcepcionRegla("Inventory entries are immutable.");
            if (entry.Entity is AsignacionCompra allocation && entry.State != EntityState.Added)
            {
                if (entry.State == EntityState.Deleted || entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not nameof(AsignacionCompra.Liberada) and not nameof(EntidadEmpresa.Revision)))
                    throw new ExcepcionRegla("Allocation audit is immutable.");
                if (!allocation.Liberada) throw new ExcepcionRegla("Released allocations cannot be restored.");
            }
        }
        foreach (var entry in changed.Where(x => x.Entity is AsignacionCompra))
        {
            var allocation = (AsignacionCompra)entry.Entity;
            var line = ChangeTracker.Entries<DetallePedidoCompra>().Select(x => x.Entity).SingleOrDefault(x => x.Id == allocation.DetallePedidoCompraId)
                ?? await DetallesPedidoCompra.SingleAsync(x => x.Id == allocation.DetallePedidoCompraId);
            var need = ChangeTracker.Entries<NecesidadIngrediente>().Select(x => x.Entity).SingleOrDefault(x => x.Id == allocation.NecesidadIngredienteId)
                ?? await NecesidadesIngredientes.SingleAsync(x => x.Id == allocation.NecesidadIngredienteId);
            if (line.ProductoId != need.ProductoId) throw new ExcepcionRegla("Allocation products must match.");
            Cantidades.Positiva(allocation.CantidadAsignada);
            if (!changed.Any(x => x.Entity is NecesidadIngrediente r && r.Id == need.Id))
                throw new ExcepcionRegla("Allocation changes must update the saved requirement reservation atomically.");
            if (entry.State != EntityState.Added)
            {
                var order = ChangeTracker.Entries<PedidoCompra>().Select(x => x.Entity).SingleOrDefault(x => x.Id == line.PedidoCompraId);
                if (order?.Estado != EstadoPedidoCompra.Rechazado && !(order?.Estado == EstadoPedidoCompra.Borrador && line.Asignaciones.All(x => x.Liberada)))
                    throw new ExcepcionRegla("Only rejection or explicit draft-line removal may release an allocation.");
            }
            else if (await PedidosCompra.AnyAsync(x => x.Id == line.PedidoCompraId && x.Estado != EstadoPedidoCompra.Borrador))
                throw new ExcepcionRegla("Only draft purchases accept new allocations.");
        }
        foreach (var entry in changed.Where(x => x.Entity is NecesidadIngrediente))
        {
            var need = (NecesidadIngrediente)entry.Entity;
            var saved = await AsignacionesCompra.AsNoTracking().Where(x => x.NecesidadIngredienteId == need.Id).ToListAsync();
            var pending = changed.Where(x => x.Entity is AsignacionCompra a && a.NecesidadIngredienteId == need.Id).ToList();
            var pendingIds = pending.Select(x => ((AsignacionCompra)x.Entity).Id).ToHashSet();
            var total = saved.Where(x => !pendingIds.Contains(x.Id) && !x.Liberada).Sum(x => x.CantidadAsignada)
                + pending.Where(x => x.State != EntityState.Deleted).Select(x => (AsignacionCompra)x.Entity).Where(x => !x.Liberada).Sum(x => x.CantidadAsignada);
            if (need.CantidadAsignada != total || need.CantidadAsignada > need.CantidadNecesaria || need.CantidadAsignada < 0)
                throw new ExcepcionRegla("Saved allocation totals must match the requirement reservation.");
        }
    }

    private async Task ValidarNuevaOrdenAsync(OrdenRecepcion orden)
    {
        if (orden.Estado != EstadoOrdenRecepcion.Pendiente || orden.FechaCreacion == default)
            throw new ExcepcionRegla("A new receipt order must have pending status and a creation timestamp.");
        ServicioFlujo.Texto(orden.NombreProveedor); ServicioFlujo.Texto(orden.NombreEstablecimiento);
        var pedido = ChangeTracker.Entries<PedidoCompra>().Where(x => x.State != EntityState.Deleted).Select(x => x.Entity).FirstOrDefault(x => x.Id == orden.PedidoCompraId)
            ?? await PedidosCompra.IgnoreQueryFilters().Include(x => x.Detalles).ThenInclude(x => x.Asignaciones)
                .SingleOrDefaultAsync(x => x.EmpresaId == orden.EmpresaId && x.Id == orden.PedidoCompraId);
        if (pedido is null || pedido.Estado != EstadoPedidoCompra.Enviado || pedido.ProveedorId != orden.ProveedorId || pedido.EstablecimientoId != orden.EstablecimientoId)
            throw new ExcepcionRegla("A receipt order must match a sent purchase and its destination.");
        var proveedor = await Proveedores.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.EmpresaId == orden.EmpresaId && x.Id == orden.ProveedorId);
        var destino = await Establecimientos.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.EmpresaId == orden.EmpresaId && x.Id == orden.EstablecimientoId);
        if (proveedor?.Nombre != orden.NombreProveedor || destino?.Nombre != orden.NombreEstablecimiento)
            throw new ExcepcionRegla("Receipt order company snapshots must match the source purchase.");
        var lineas = ReglasPedidoCompra.DetallesActivos(pedido).ToArray();
        if (lineas.Length == 0 || lineas.Length != orden.Detalles.Count)
            throw new ExcepcionRegla("Receipt order details must match every active purchase line atomically.");
        foreach (var detalle in orden.Detalles)
        {
            var original = lineas.FirstOrDefault(x => x.ProductoId == detalle.ProductoId && x.PresentacionProveedorId == detalle.PresentacionProveedorId &&
                x.NombrePresentacion == detalle.NombrePresentacion && x.UnidadBaseId == detalle.UnidadBaseId &&
                x.CantidadPresentaciones == detalle.CantidadPresentaciones && x.CantidadBasePresentacion == detalle.CantidadBasePresentacion && x.CantidadPedida == detalle.CantidadEsperada);
            var producto = await Productos.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.EmpresaId == orden.EmpresaId && x.Id == detalle.ProductoId);
            if (original is null || producto?.Nombre != detalle.NombreProducto)
                throw new ExcepcionRegla("Receipt order details must preserve valid purchase snapshots.");
        }
    }

    private Task ValidarNuevoDetalleOrdenAsync(DetalleOrdenRecepcion detalle)
    {
        if (!ChangeTracker.Entries<OrdenRecepcion>().Any(x => x.State == EntityState.Added && x.Entity.Id == detalle.OrdenRecepcionId))
            throw new ExcepcionRegla("Receipt order details must be created atomically with their order.");
        if (detalle.CantidadEsperada != Cantidades.CantidadPresentaciones(detalle.CantidadPresentaciones, detalle.CantidadBasePresentacion))
            throw new ExcepcionRegla("Receipt order expected quantities must equal whole packs times base content.");
        ServicioFlujo.Texto(detalle.NombreProducto); ServicioFlujo.Texto(detalle.NombrePresentacion); ServicioFlujo.Texto(detalle.UnidadBaseId, 32);
        return Task.CompletedTask;
    }

    private async Task BuscarDestinoValidoAsync(Guid destinoId, Guid empresaId)
    {
        if (destinoId == Guid.Empty || !await Establecimientos.IgnoreQueryFilters().AnyAsync(x => x.EmpresaId == empresaId && x.Id == destinoId))
            throw new ExcepcionRegla("The purchase destination must belong to the same company.");
    }
}
