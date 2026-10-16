using System.Security.Claims;
using FoodTrace.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FoodTrace.API.Infrastructure.Identity;

public static class Roles
{
    public const string Administrador = "admin";
    public const string Planificador = "planner";
    public const string Comprador = "buyer";
    public const string Aprobador = "approver";
    public static readonly string[] Todos = [Administrador, Planificador, Comprador, Aprobador];
}

public sealed class SesionEmpresa(IHttpContextAccessor accesoContexto)
{
    public Guid EmpresaId => Guid.TryParse(accesoContexto.HttpContext?.User.FindFirstValue("company"), out var id)
        ? id : Guid.Empty;
    public string UsuarioId => accesoContexto.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();
}

public sealed class FabricaClaimsEmpresa(UserManager<AppUser> usuarios, RoleManager<IdentityRole> roles,
    IOptions<IdentityOptions> opciones) : UserClaimsPrincipalFactory<AppUser, IdentityRole>(usuarios, roles, opciones)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser usuario)
    {
        var identidad = await base.GenerateClaimsAsync(usuario);
        identidad.AddClaim(new Claim("company", usuario.EmpresaId.ToString()));
        return identidad;
    }
}

// Cada petición verifica la membresía persistida y el sello de seguridad del token.
public sealed class MembresiaMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto, UserManager<AppUser> usuarios)
    {
        if (contexto.User.Identity?.IsAuthenticated == true)
        {
            var usuario = await usuarios.GetUserAsync(contexto.User);
            var sello = contexto.User.FindFirstValue("AspNet.Identity.SecurityStamp");
            if (usuario is null || !usuario.Habilitado || usuario.EmpresaId.ToString() != contexto.User.FindFirstValue("company")
                || sello != await usuarios.GetSecurityStampAsync(usuario))
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }
        await siguiente(contexto);
    }
}
