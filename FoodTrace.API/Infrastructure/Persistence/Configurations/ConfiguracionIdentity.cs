using FoodTrace.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Persistence.Configurations;

internal static class ConfiguracionIdentity
{
    public static void ConfigurarLongitudes(ModelBuilder b)
    {
        // Tres columnas nvarchar(128) suman 768 bytes, por debajo del límite PK de 900 bytes.
        b.Entity<AppUser>().Property(x => x.Id).HasMaxLength(128);
        b.Entity<AppUser>().Property(x => x.EmpresaId).HasColumnName("CompanyId");
        b.Entity<AppUser>().Property(x => x.Habilitado).HasColumnName("Enabled");
        b.Entity<AppUser>().Property(x => x.UserName).HasMaxLength(256);
        b.Entity<AppUser>().Property(x => x.NormalizedUserName).HasMaxLength(256);
        b.Entity<AppUser>().Property(x => x.Email).HasMaxLength(256);
        b.Entity<AppUser>().Property(x => x.NormalizedEmail).HasMaxLength(256);
        b.Entity<IdentityRole>().Property(x => x.Id).HasMaxLength(128);
        b.Entity<IdentityRole>().Property(x => x.Name).HasMaxLength(256);
        b.Entity<IdentityRole>().Property(x => x.NormalizedName).HasMaxLength(256);
        b.Entity<IdentityUserClaim<string>>().Property(x => x.UserId).HasMaxLength(128);
        b.Entity<IdentityRoleClaim<string>>().Property(x => x.RoleId).HasMaxLength(128);
        b.Entity<IdentityUserRole<string>>().Property(x => x.UserId).HasMaxLength(128);
        b.Entity<IdentityUserRole<string>>().Property(x => x.RoleId).HasMaxLength(128);
        b.Entity<IdentityUserLogin<string>>().Property(x => x.UserId).HasMaxLength(128);
        b.Entity<IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        b.Entity<IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        b.Entity<IdentityUserToken<string>>().Property(x => x.UserId).HasMaxLength(128);
        b.Entity<IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        b.Entity<IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);
        b.Entity<OrdenProduccion>().Property(x => x.CreadoPorId).HasMaxLength(128);
        b.Entity<PedidoCompra>().Property(x => x.CreadoPorId).HasMaxLength(128);
        b.Entity<PedidoCompra>().Property(x => x.EnviadoPorId).HasMaxLength(128);
        b.Entity<PedidoCompra>().Property(x => x.DecididoPorId).HasMaxLength(128);
    }
}
