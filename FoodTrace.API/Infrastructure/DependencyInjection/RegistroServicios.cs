using FoodTrace.API.Application.Interfaces;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Configuration;
using FoodTrace.API.Infrastructure.Validation;
using FoodTrace.API.Infrastructure.Persistence;
using FoodTrace.API.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.DependencyInjection;

public static class RegistroServicios
{
    public static IServiceCollection AgregarFoodTrace(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AgregarErroresFoodTrace();
        servicios.AddControllers(opciones =>
        {
            opciones.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            opciones.ModelMetadataDetailsProviders.Add(new MetadatosContratoJson());
        });
        servicios.AddHttpContextAccessor();
        servicios.AddScoped<SesionEmpresa>();
        servicios.AddScoped<IServicioFlujo, ServicioFlujo>();
        servicios.AddScoped<IServicioCatalogos, ServicioCatalogos>();
        servicios.AddScoped<IServicioUsuarios, ServicioUsuarios>();
        var conexion = ConfiguracionFoodTrace.ObtenerConexion(configuracion);
        servicios.AddDbContext<FoodTraceDbContext>(opciones => opciones.UseSqlServer(conexion));
        servicios.AddIdentityCore<AppUser>(opciones =>
        {
            opciones.User.RequireUniqueEmail = true;
            opciones.Password.RequiredLength = 12;
            opciones.Lockout.MaxFailedAccessAttempts = 5;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<FoodTraceDbContext>().AddSignInManager()
            .AddClaimsPrincipalFactory<FabricaClaimsEmpresa>();
        servicios.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme,
            opciones => opciones.BearerTokenExpiration = TimeSpan.FromMinutes(30));
        servicios.AddAuthorization(opciones => opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(IdentityConstants.BearerScheme).RequireAuthenticatedUser().Build());
        return servicios;
    }
}
