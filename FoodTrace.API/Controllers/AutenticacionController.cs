using FoodTrace.API.Dtos.Autenticacion;
using FoodTrace.API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrace.API.Controllers;

[ApiController, Route("auth")]
public sealed class AutenticacionController(UserManager<AppUser> usuarios, SignInManager<AppUser> inicioSesion) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> IniciarSesionAsync(IniciarSesionDto entrada, CancellationToken cancelacion = default)
    {
        var usuario = await usuarios.FindByEmailAsync(entrada.Correo);
        if (usuario is null || !usuario.Habilitado) return Unauthorized();
        var resultado = await inicioSesion.CheckPasswordSignInAsync(usuario, entrada.Contrasena, lockoutOnFailure: true);
        if (!resultado.Succeeded) return Unauthorized();
        var principal = await inicioSesion.CreateUserPrincipalAsync(usuario);
        return SignIn(principal, IdentityConstants.BearerScheme);
    }
}
