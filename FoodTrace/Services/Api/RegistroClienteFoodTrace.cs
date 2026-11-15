using FoodTrace.Services.Interfaces;
using FoodTrace.Services.Estado;

namespace FoodTrace.Services.Api;

public static class RegistroClienteFoodTrace
{
    public static IServiceCollection AgregarClienteFoodTrace(this IServiceCollection servicios)
    {
        servicios.AddScoped(proveedor => new ClienteFoodTraceApi(
            proveedor.GetRequiredService<IHttpClientFactory>().CreateClient("FoodTrace"),
            proveedor.GetRequiredService<ILogger<ClienteFoodTraceApi>>()));
        servicios.AddScoped<ISesionFoodTrace>(proveedor => proveedor.GetRequiredService<ClienteFoodTraceApi>());
        servicios.AddScoped<ICatalogosFoodTrace>(proveedor => proveedor.GetRequiredService<ClienteFoodTraceApi>());
        servicios.AddScoped<IUsuariosFoodTrace>(proveedor => proveedor.GetRequiredService<ClienteFoodTraceApi>());
        servicios.AddScoped<IServicioRecetas>(proveedor => proveedor.GetRequiredService<ClienteFoodTraceApi>());
        servicios.AddScoped<IServicioProduccion>(proveedor => proveedor.GetRequiredService<ClienteFoodTraceApi>());
        servicios.AddScoped<IServicioCompras>(proveedor => proveedor.GetRequiredService<ClienteFoodTraceApi>());
        servicios.AddScoped<EstadoAdministracion>();
        servicios.AddScoped<EstadoNavegacion>();
        servicios.AddScoped<EstadoInicio>();
        return servicios;
    }
}
