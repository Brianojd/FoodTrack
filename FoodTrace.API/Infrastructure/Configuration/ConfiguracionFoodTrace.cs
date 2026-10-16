using Microsoft.Extensions.Configuration;

namespace FoodTrace.API.Infrastructure.Configuration;

public static class ConfiguracionFoodTrace
{
    public static string ObtenerConexion(IConfiguration configuracion)
    {
        var conexion = configuracion.GetConnectionString("FoodTrace");
        if (string.IsNullOrWhiteSpace(conexion))
            throw new InvalidOperationException("Falta configurar ConnectionStrings:FoodTrace.");
        return conexion;
    }

    public static IConfigurationRoot CrearParaDiseno(string[] argumentos)
    {
        var entorno = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Development;
        var indice = Array.IndexOf(argumentos, "--environment");
        if (indice >= 0)
        {
            if (indice + 1 >= argumentos.Length || string.IsNullOrWhiteSpace(argumentos[indice + 1]))
                throw new InvalidOperationException("--environment requiere un entorno.");
            entorno = argumentos[indice + 1];
        }
        var configuracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{entorno}.json", optional: true);
        if (string.Equals(entorno, Environments.Development, StringComparison.OrdinalIgnoreCase))
            configuracion.AddUserSecrets(typeof(ConfiguracionFoodTrace).Assembly, optional: true);
        return configuracion.AddEnvironmentVariables().AddCommandLine(argumentos).Build();
    }
}
