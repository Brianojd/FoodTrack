using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FoodTrace.API.Infrastructure.Persistence;

public sealed class FoodTraceDbContextFactory
    : IDesignTimeDbContextFactory<FoodTraceDbContext>
{
    public FoodTraceDbContext CreateDbContext(string[] args)
    {
        var configuration = ConfiguracionFoodTrace.CrearParaDiseno(args);
        var connectionString = ConfiguracionFoodTrace.ObtenerConexion(configuration);

        var options = new DbContextOptionsBuilder<FoodTraceDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FoodTraceDbContext(
            options,
            new SesionEmpresa(new HttpContextAccessor()));
    }
}
