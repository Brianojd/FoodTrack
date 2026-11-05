using FoodTrace.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Persistence.Configurations;

internal static class ConfiguracionModelo
{
    // El orden explícito mantiene el modelo determinista, sin descubrimiento por ensamblado.
    public static void Configurar(ModelBuilder b, FoodTraceDbContext context)
    {
        ConfiguracionIdentity.ConfigurarLongitudes(b);
        ConfiguracionEsquemaEspanol.Configurar(b);
        ConfiguracionRestricciones.Configurar(b);
        b.Entity<UnidadMedida>().Property(x => x.Id).HasMaxLength(32);
        b.Entity<UnidadMedida>().Property(x => x.Dimension).HasMaxLength(32);
        b.Entity<Producto>().Property(x => x.UnidadBaseId).HasMaxLength(32);
        b.Entity<PresentacionProveedor>().Property(x => x.UnidadMedidaId).HasMaxLength(32);
        b.Entity<IngredienteReceta>().Property(x => x.UnidadMedidaId).HasMaxLength(32);
        b.Entity<VersionReceta>().Property(x => x.UnidadRendimientoId).HasMaxLength(32);
        b.Entity<VersionReceta>().HasOne<UnidadMedida>().WithMany().HasForeignKey(x => x.UnidadRendimientoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<NecesidadIngrediente>().Property(x => x.UnidadBaseId).HasMaxLength(32);
        b.Entity<DetallePedidoCompra>().Property(x => x.UnidadBaseId).HasMaxLength(32);
        ConfigurarEmpresa<Establecimiento>(b, context); ConfigurarEmpresa<Producto>(b, context); ConfigurarEmpresa<Proveedor>(b, context);
        ConfigurarEmpresa<PresentacionProveedor>(b, context); ConfigurarEmpresa<VersionReceta>(b, context); ConfigurarEmpresa<IngredienteReceta>(b, context);
        ConfigurarEmpresa<OrdenProduccion>(b, context); ConfigurarEmpresa<NecesidadIngrediente>(b, context); ConfigurarEmpresa<PedidoCompra>(b, context);
        ConfigurarEmpresa<DetallePedidoCompra>(b, context); ConfigurarEmpresa<AsignacionCompra>(b, context);
        b.Entity<AppUser>().HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AppUser>().HasAlternateKey(x => new { x.EmpresaId, x.Id }).IsClustered(false);
        b.Entity<Producto>().HasOne<UnidadMedida>().WithMany().HasForeignKey(x => x.UnidadBaseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PresentacionProveedor>().HasOne<Proveedor>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.ProveedorId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        ReferenciaProducto<PresentacionProveedor>(b, nameof(PresentacionProveedor.ProductoId));
        b.Entity<PresentacionProveedor>().HasOne<UnidadMedida>().WithMany().HasForeignKey(x => x.UnidadMedidaId).OnDelete(DeleteBehavior.Restrict);
        ReferenciaProducto<VersionReceta>(b, nameof(VersionReceta.ProductoResultadoId));
        b.Entity<VersionReceta>().HasIndex(x => new { x.EmpresaId, x.FamiliaRecetaId, x.NumeroVersion }).IsUnique();
        b.Entity<VersionReceta>().HasMany(x => x.Ingredientes).WithOne().HasForeignKey(x => new { x.EmpresaId, x.VersionRecetaId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        ReferenciaProducto<IngredienteReceta>(b, nameof(IngredienteReceta.ProductoId));
        b.Entity<IngredienteReceta>().HasOne<UnidadMedida>().WithMany().HasForeignKey(x => x.UnidadMedidaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrdenProduccion>().HasOne<VersionReceta>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.VersionRecetaId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrdenProduccion>().HasOne<Establecimiento>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrdenProduccion>().HasMany(x => x.Necesidades).WithOne().HasForeignKey(x => new { x.EmpresaId, x.OrdenProduccionId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        ReferenciaProducto<NecesidadIngrediente>(b, nameof(NecesidadIngrediente.ProductoId));
        b.Entity<NecesidadIngrediente>().HasOne<UnidadMedida>().WithMany().HasForeignKey(x => x.UnidadBaseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PedidoCompra>().HasOne<Proveedor>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.ProveedorId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PedidoCompra>().HasMany(x => x.Detalles).WithOne().HasForeignKey(x => new { x.EmpresaId, x.PedidoCompraId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DetallePedidoCompra>().HasOne<PresentacionProveedor>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.PresentacionProveedorId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        ReferenciaProducto<DetallePedidoCompra>(b, nameof(DetallePedidoCompra.ProductoId));
        b.Entity<DetallePedidoCompra>().HasMany(x => x.Asignaciones).WithOne().HasForeignKey(x => new { x.EmpresaId, x.DetallePedidoCompraId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AsignacionCompra>().HasOne<NecesidadIngrediente>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.NecesidadIngredienteId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrdenProduccion>().HasOne<AppUser>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.CreadoPorId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PedidoCompra>().HasOne<AppUser>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.CreadoPorId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PedidoCompra>().HasOne<AppUser>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.EnviadoPorId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PedidoCompra>().HasOne<AppUser>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.DecididoPorId })
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<UnidadMedida>().HasData(
            new UnidadMedida { Id = "kg", Dimension = "mass", FactorConversion = 1 },
            new UnidadMedida { Id = "g", Dimension = "mass", FactorConversion = 0.001m },
            new UnidadMedida { Id = "L", Dimension = "volume", FactorConversion = 1 },
            new UnidadMedida { Id = "ml", Dimension = "volume", FactorConversion = 0.001m },
            new UnidadMedida { Id = "unit", Dimension = "count", FactorConversion = 1 });
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?)) { property.SetPrecision(18); property.SetScale(6); }
                if (property.ClrType == typeof(string) && property.GetMaxLength() is null)
                    property.SetMaxLength(property.Name == nameof(VersionReceta.Instrucciones) ? 8000 : 500);
            }
    }

    private static void ConfigurarEmpresa<T>(ModelBuilder b, FoodTraceDbContext context) where T : EntidadEmpresa
    {
        b.Entity<T>().HasKey(x => x.Id);
        b.Entity<T>().HasAlternateKey(x => new { x.EmpresaId, x.Id });
        b.Entity<T>().Property(x => x.Revision).IsConcurrencyToken();
        // El filtro referencia el contexto activo, sin capturar la empresa al construir el modelo.
        b.Entity<T>().HasQueryFilter(x => x.EmpresaId == context.EmpresaActualId);
        b.Entity<T>().HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ReferenciaProducto<T>(ModelBuilder b, string name) where T : EntidadEmpresa =>
        b.Entity<T>().HasOne<Producto>().WithMany().HasForeignKey(nameof(EntidadEmpresa.EmpresaId), name)
            .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
}
