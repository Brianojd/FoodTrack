using System;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Dtos.Comunes;
using FoodTrace.API.Dtos.Produccion;
using FoodTrace.API.Dtos.Recetas;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FoodTrace.Tests;

public sealed class RecipeFormFunctionalTests
{
    [Fact]
    public async Task Planificar_por_cantidad_rechaza_conversion_subprecisa_sin_mutar_y_conserva_fracciones_exactas()
    {
        using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var empresa = Guid.NewGuid();
        var sesion = new SesionEmpresa(new HttpContextAccessor { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("company", empresa.ToString()), new Claim(ClaimTypes.NameIdentifier, "qa") })) } });
        await using var db = new FoodTraceDbContext(new DbContextOptionsBuilder<FoodTraceDbContext>().UseSqlite(conexion).Options, sesion);
        await db.Database.EnsureCreatedAsync();
        var producto = new Producto { EmpresaId = empresa, Nombre = "QA-Recetas precisión plan", UnidadBaseId = "kg" };
        var sitio = new Establecimiento { EmpresaId = empresa, Nombre = "QA-Recetas cocina" };
        db.Empresas.Add(new Empresa { Id = empresa, Nombre = "QA-Recetas precisión" });
        db.Users.Add(new AppUser { Id = "qa", EmpresaId = empresa, UserName = "QA-Recetas" });
        db.Productos.Add(producto); db.Establecimientos.Add(sitio); await db.SaveChangesAsync();
        var flujo = new ServicioFlujo(db, sesion);
        var receta = await flujo.CrearRecetaAsync(new CrearRecetaDto
        {
            Nombre = "QA-Recetas precisión", ProductoResultadoId = producto.Id, Rendimiento = 1, Instrucciones = "Mezclar.",
            Ingredientes = new[] { new IngredienteRecetaEntradaDto { ProductoId = producto.Id, Cantidad = 1, UnidadMedidaId = "kg" } }
        });
        receta = await flujo.PublicarRecetaAsync(receta.Id, new RevisionDto { Revision = receta.Revision });
        PlanificarProduccionDto Entrada(decimal cantidad, string unidad) => new()
        { EstablecimientoId = sitio.Id, VersionRecetaId = receta.Id, CantidadPlanificada = cantidad, UnidadMedidaId = unidad, FechaPlanificada = new DateOnly(2026, 11, 5) };
        var error = await Assert.ThrowsAsync<ExcepcionRegla>(() => flujo.PlanificarProduccionAsync(Entrada(.000001m, "g")));
        Assert.Contains("Quantity", error.Errores!.Keys);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(0, await db.OrdenesProduccion.CountAsync());
        Assert.Equal(0, await db.NecesidadesIngredientes.CountAsync());
        var minima = await flujo.PlanificarProduccionAsync(Entrada(.001m, "g"));
        Assert.Equal(.000001m, minima.CantidadPlanificada);
        Assert.Equal(.000001m, Assert.Single(minima.Necesidades).CantidadNecesaria);
        var fraccion = await flujo.PlanificarProduccionAsync(Entrada(1.5m, "kg"));
        Assert.Equal(1.5m, fraccion.CantidadPlanificada);
        Assert.Equal(1.5m, Assert.Single(fraccion.Necesidades).CantidadNecesaria);
    }

    [Fact]
    public async Task Las_filas_repetidas_se_suman_antes_del_redondeo_final()
    {
        using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var empresa = Guid.NewGuid();
        var contextoHttp = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("company", empresa.ToString()), new Claim(ClaimTypes.NameIdentifier, "qa")
            }))
        };
        var sesion = new SesionEmpresa(new HttpContextAccessor { HttpContext = contextoHttp });
        await using var db = new FoodTraceDbContext(new DbContextOptionsBuilder<FoodTraceDbContext>()
            .UseSqlite(conexion).Options, sesion);
        await db.Database.EnsureCreatedAsync();
        var producto = new Producto { EmpresaId = empresa, Nombre = "QA-Recetas precisión", UnidadBaseId = "kg" };
        var sitio = new Establecimiento { EmpresaId = empresa, Nombre = "QA-Recetas cocina" };
        db.Empresas.Add(new Empresa { Id = empresa, Nombre = "QA-Recetas" });
        db.Users.Add(new AppUser { Id = "qa", EmpresaId = empresa, UserName = "QA-Recetas" });
        db.Productos.Add(producto);
        db.Establecimientos.Add(sitio);
        await db.SaveChangesAsync();
        var flujo = new ServicioFlujo(db, sesion);
        var receta = await flujo.CrearRecetaAsync(new CrearRecetaDto
        {
            Nombre = "QA-Recetas duplicados", ProductoResultadoId = producto.Id, Rendimiento = 3,
            Instrucciones = "Mezclar.", Ingredientes = new[]
            {
                new IngredienteRecetaEntradaDto { ProductoId = producto.Id, UnidadMedidaId = "kg", Cantidad = 1 },
                new IngredienteRecetaEntradaDto { ProductoId = producto.Id, UnidadMedidaId = "kg", Cantidad = 1 }
            }
        });
        receta = await flujo.PublicarRecetaAsync(receta.Id, new RevisionDto { Revision = receta.Revision });
        var orden = await flujo.PlanificarProduccionAsync(new PlanificarProduccionDto
        {
            EstablecimientoId = sitio.Id, VersionRecetaId = receta.Id, CantidadPlanificada = 1,
            UnidadMedidaId = "kg", FechaPlanificada = new DateOnly(2026, 11, 4)
        });
        Assert.Equal(0.666667m, Assert.Single(orden.Necesidades).CantidadNecesaria);
    }

    [Fact]
    public async Task Una_edicion_invalida_no_modifica_el_borrador_rastreado()
    {
        using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var empresa = Guid.NewGuid();
        var sesion = new SesionEmpresa(new HttpContextAccessor { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("company", empresa.ToString()) })) } });
        await using var db = new FoodTraceDbContext(new DbContextOptionsBuilder<FoodTraceDbContext>().UseSqlite(conexion).Options, sesion);
        await db.Database.EnsureCreatedAsync();
        var producto = new Producto { EmpresaId = empresa, Nombre = "QA-Recetas masa", UnidadBaseId = "kg" };
        db.Empresas.Add(new Empresa { Id = empresa, Nombre = "QA-Recetas" });
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        var flujo = new ServicioFlujo(db, sesion);
        var receta = await flujo.CrearRecetaAsync(new CrearRecetaDto
        {
            Nombre = "Original", ProductoResultadoId = producto.Id, Rendimiento = 1500, UnidadRendimientoId = "g",
            PorcionesRendimiento = 10, Instrucciones = "Mezclar.",
            Ingredientes = new[] { new IngredienteRecetaEntradaDto { ProductoId = producto.Id, Cantidad = 1, UnidadMedidaId = "kg" } }
        });
        Assert.Equal(1.5m, receta.Rendimiento);
        Assert.Equal(1500m, receta.CantidadRendimiento);
        var error = await Assert.ThrowsAsync<ExcepcionRegla>(() => flujo.ActualizarRecetaAsync(receta.Id, new ActualizarRecetaDto
        {
            Revision = receta.Revision, Nombre = "No debe cambiar", ProductoResultadoId = producto.Id,
            Rendimiento = .000001m, UnidadRendimientoId = "g", PorcionesRendimiento = -1, Instrucciones = "Cambiar.",
            Ingredientes = new[]
            {
                new IngredienteRecetaEntradaDto { ProductoId = Guid.Empty, Cantidad = 0, UnidadMedidaId = "L" },
                new IngredienteRecetaEntradaDto { ProductoId = producto.Id, Cantidad = -1, UnidadMedidaId = "L" }
            }
        }));
        Assert.Contains("Yield", error.Errores!.Keys);
        Assert.Contains("YieldPortions", error.Errores.Keys);
        Assert.Contains("Ingredients[0].ProductId", error.Errores.Keys);
        Assert.Contains("Ingredients[0].Quantity", error.Errores.Keys);
        Assert.Contains("Ingredients[1].UnitId", error.Errores.Keys);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal("Original", db.VersionesReceta.Local.Single().Nombre);
        Assert.Single(db.IngredientesReceta.Local);
        Assert.Equal(receta.Revision, db.VersionesReceta.Local.Single().Revision);
        var productoGramos = new Producto { EmpresaId = empresa, Nombre = "QA-Recetas gramos", UnidadBaseId = "g" };
        db.Productos.Add(productoGramos);
        await db.SaveChangesAsync();
        var exceso = await Assert.ThrowsAsync<ExcepcionRegla>(() => flujo.CrearRecetaAsync(new CrearRecetaDto
        {
            Nombre = "Fuera de rango", ProductoResultadoId = producto.Id, Rendimiento = 1, Instrucciones = "Mezclar.",
            Ingredientes = new[] { new IngredienteRecetaEntradaDto { ProductoId = productoGramos.Id, Cantidad = 1000000000m, UnidadMedidaId = "kg" } }
        }));
        Assert.Contains("Ingredients[0].Quantity", exceso.Errores!.Keys);
        Assert.False(db.ChangeTracker.HasChanges());
        var publicada = await flujo.PublicarRecetaAsync(receta.Id, new RevisionDto { Revision = receta.Revision });
        var siguiente = await flujo.CrearVersionRecetaAsync(receta.Id, new RevisionDto { Revision = publicada.Revision });
        Assert.Equal(1500m, siguiente.CantidadRendimiento);
        Assert.Equal("g", siguiente.UnidadRendimientoId);
        Assert.Equal(10m, siguiente.PorcionesRendimiento);
        db.VersionesReceta.Local.Single(x => x.Id == receta.Id).PorcionesRendimiento = 20;
        await Assert.ThrowsAsync<ExcepcionRegla>(() => db.SaveChangesAsync());
    }
}
