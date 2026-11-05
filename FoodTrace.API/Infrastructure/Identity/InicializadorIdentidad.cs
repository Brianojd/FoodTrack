using System.Data;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Infrastructure.Persistence;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Identity;

public static class InicializadorIdentidad
{
    public static async Task EjecutarAsync(IServiceProvider servicios, IConfiguration configuracion)
    {
        // Se ejecuta solo con --bootstrap y secretos de usuario o variables de entorno, nunca JSON.
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string Obligatorio(string clave) => ServicioFlujo.Texto(configuracion[$"Bootstrap:{clave}"]);
        var empresaId = Guid.Parse(Obligatorio("CompanyId"));
        var nombreEmpresa = Obligatorio("CompanyName"); var correo = Obligatorio("AdminEmail"); var contrasena = Obligatorio("AdminPassword");
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var existente = await usuarios.FindByEmailAsync(correo);
        if (existente is not null)
        {
            if (existente.EmpresaId != empresaId || !await usuarios.IsInRoleAsync(existente, "admin"))
                throw new InvalidOperationException("Bootstrap identity conflicts with existing membership.");
            await transaccion.CommitAsync(); return;
        }
        if (!await db.Empresas.AnyAsync(x => x.Id == empresaId))
        {
            db.Empresas.Add(new Empresa { Id = empresaId, Nombre = nombreEmpresa }); await db.SaveChangesAsync();
        }
        foreach (var role in Roles.Todos)
            if (!await roles.RoleExistsAsync(role)) ComprobarResultado(await roles.CreateAsync(new IdentityRole(role)));
        var administrador = new AppUser { EmpresaId = empresaId, UserName = correo, Email = correo };
        ComprobarResultado(await usuarios.CreateAsync(administrador, contrasena)); ComprobarResultado(await usuarios.AddToRoleAsync(administrador, "admin"));
        await transaccion.CommitAsync();
    }

    public static void ComprobarResultado(IdentityResult resultado)
    {
        if (!resultado.Succeeded) throw new ExcepcionRegla(string.Join(" ", resultado.Errors.Select(x => x.Description)));
    }
}
