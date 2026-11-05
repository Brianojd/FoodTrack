using FoodTrace.API.Application.Interfaces;
using FoodTrace.API.Dtos.Usuarios;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Application.Services;

public sealed class ServicioUsuarios(FoodTraceDbContext db, SesionEmpresa sesion, UserManager<AppUser> usuarios) : IServicioUsuarios
{
    public async Task<List<UsuarioDto>> ObtenerUsuariosAsync(CancellationToken cancelacion = default)
    {
        var resultado = new List<UsuarioDto>();
        foreach (var usuario in await db.Users.Where(x => x.EmpresaId == sesion.EmpresaId).ToListAsync(cancelacion))
            resultado.Add(new() { Id = usuario.Id, Correo = usuario.Email, Habilitado = usuario.Habilitado, Roles = await usuarios.GetRolesAsync(usuario) });
        return resultado;
    }

    public async Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto entrada, CancellationToken cancelacion = default)
    {
        ValidarRoles(entrada.Roles);
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var correo = ServicioFlujo.Texto(entrada.Correo);
        var usuario = new AppUser { EmpresaId = sesion.EmpresaId, Email = correo, UserName = correo };
        InicializadorIdentidad.ComprobarResultado(await usuarios.CreateAsync(usuario, entrada.Contrasena));
        InicializadorIdentidad.ComprobarResultado(await usuarios.AddToRolesAsync(usuario, entrada.Roles));
        await transaccion.CommitAsync(cancelacion); return new() { Id = usuario.Id, Correo = usuario.Email, Habilitado = usuario.Habilitado, Roles = entrada.Roles };
    }

    public async Task<UsuarioDto> ActualizarMembresiaAsync(string id, ActualizarMembresiaDto entrada, CancellationToken cancelacion = default)
    {
        ValidarRoles(entrada.Roles);
        var usuario = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.EmpresaId == sesion.EmpresaId, cancelacion)
            ?? throw new KeyNotFoundException();
        if (usuario.Id == sesion.UsuarioId) throw new ExcepcionRegla("Ask another administrator to change your own membership.");
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        InicializadorIdentidad.ComprobarResultado(await usuarios.RemoveFromRolesAsync(usuario, await usuarios.GetRolesAsync(usuario)));
        InicializadorIdentidad.ComprobarResultado(await usuarios.AddToRolesAsync(usuario, entrada.Roles));
        usuario.Habilitado = entrada.Habilitado;
        InicializadorIdentidad.ComprobarResultado(await usuarios.UpdateSecurityStampAsync(usuario));
        await transaccion.CommitAsync(cancelacion); return new() { Id = usuario.Id, Correo = usuario.Email, Habilitado = usuario.Habilitado, Roles = entrada.Roles };
    }

    private static void ValidarRoles(string[] roles)
    {
        if (roles.Length == 0 || roles.Distinct().Count() != roles.Length || roles.Except(Roles.Todos).Any())
            throw new ExcepcionRegla("Select unique roles from admin, planner, buyer, approver.");
    }
}
