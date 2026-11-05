using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FoodTrace.Tests;

// El mismo recorrido HTTP puede ejecutarse con el host real y la base SQL QA guardada.
// El runner estándar conserva su fixture SQLite; no crea bases SQL implícitamente.
public sealed class RecipeSqlServerTests
{
    [Fact]
    public async Task Cinco_familias_conservan_unidades_versiones_y_necesidades()
    {
        using var app = new ApiFixture();
        await app.Seed();
        using var cliente = await app.Login("admin@test.invalid");
        await VerificarFamiliasAsync(cliente);
    }

    [Fact]
    public async Task Validacion_indexada_compatibilidad_y_aislamiento_con_roles_reales()
    {
        using var app = new ApiFixture();
        await app.Seed();
        using var cliente = await app.Login("admin@test.invalid");
        await EnviarAsync(cliente, "/api/users", new
        { email = "qa-planner@test.invalid", password = ApiFixture.Password, roles = new[] { "planner" } });
        await EnviarAsync(cliente, "/api/users", new
        { email = "qa-admin@test.invalid", password = ApiFixture.Password, roles = new[] { "admin" } });
        using var planificador = await app.Login("qa-planner@test.invalid");
        using var administrador = await app.Login("qa-admin@test.invalid");
        using var comprador = await app.Login("buyer@test.invalid");
        using var otra = await app.Login("other@test.invalid");
        var evidencias = await VerificarFamiliasAsync(cliente);
        await VerificarInvalidosAsync(cliente, planificador, administrador, comprador, otra, evidencias[0]);
    }

    public sealed record Evidencia(Guid RecetaId, Guid OrdenId, decimal RendimientoBase,
        decimal CantidadIngresada, string UnidadIngresada, decimal? Porciones, decimal PlanBase,
        Dictionary<Guid, decimal> Necesidades);

    public static async Task<JsonElement> EnviarAsync(HttpClient cliente, string ruta, object cuerpo, HttpMethod? metodo = null)
    {
        using var respuesta = await cliente.SendAsync(new HttpRequestMessage(metodo ?? HttpMethod.Post, ruta)
        { Content = JsonContent.Create(cuerpo) });
        // No volcar cuerpos de error, credenciales ni tokens en la salida del diagnóstico.
        Assert.True(respuesta.IsSuccessStatusCode, $"{ruta}: HTTP {(int)respuesta.StatusCode}");
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task<List<Evidencia>> VerificarFamiliasAsync(HttpClient cliente)
    {
        var evidencias = new List<Evidencia>();
        var sitio = await EnviarAsync(cliente, "/api/establishments", new { name = "QA-Recetas cocina" });
        var sitioId = sitio.GetProperty("id").GetGuid();
        var casos = new (string Nombre, string Base, decimal Cantidad, string Unidad, decimal? Porciones, decimal Plan,
            (string Base, decimal Cantidad, string Unidad, decimal Esperada)[] Ingredientes)[]
        {
            (Nombre: "Hamburguesas", Base: "unit", Cantidad: 10m, Unidad: "unit", Porciones: (decimal?)null, Plan: 100m,
                Ingredientes: new (string Base, decimal Cantidad, string Unidad, decimal Esperada)[] { ("kg", 1.5m, "kg", 15m), ("kg", 250m, "g", 2.5m), ("unit", 10m, "unit", 100m) }),
            ("Sopa", "L", 5000m, "ml", (decimal?)10m, 25m,
                new[] { ("kg", 200m, "g", 1m), ("L", 1000m, "ml", 5m) }),
            ("Pan", "kg", 1500m, "g", (decimal?)null, 3m,
                new[] { ("kg", 1000m, "g", 2m), ("L", 500m, "ml", 1m) }),
            ("Muffins", "unit", 12m, "unit", (decimal?)null, 36m,
                new[] { ("unit", 2m, "unit", 6m), ("kg", 300m, "g", .9m), ("L", 150m, "ml", .45m) }),
            ("Pizza por porciones", "unit", 1m, "unit", (decimal?)8m, 16m,
                new[] { ("kg", 200m, "g", .4m), ("kg", 100m, "g", .2m) })
        };
        foreach (var caso in casos)
        {
            var producto = await EnviarAsync(cliente, "/api/products", new { name = "QA-Recetas " + caso.Nombre, baseUnitId = caso.Base });
            var productoId = producto.GetProperty("id").GetGuid();
            var filas = new List<object>();
            var esperadas = new Dictionary<Guid, decimal>();
            foreach (var ingrediente in caso.Ingredientes)
            {
                var item = await EnviarAsync(cliente, "/api/products", new { name = "QA-Recetas ingrediente " + caso.Nombre, baseUnitId = ingrediente.Base });
                var id = item.GetProperty("id").GetGuid();
                filas.Add(new { productId = id, quantity = ingrediente.Cantidad, unitId = ingrediente.Unidad });
                esperadas[id] = ingrediente.Esperada;
            }
            object Cuerpo(Guid? revision = null, decimal? cantidad = null, string? unidad = null) => new
            {
                name = "QA-Recetas " + caso.Nombre, outputProductId = productoId,
                yield = cantidad ?? caso.Cantidad, yieldUnitId = unidad ?? caso.Unidad, yieldPortions = caso.Porciones,
                instructions = "Preparar según la fórmula QA.", ingredients = filas.ToArray(), revision
            };
            var receta = await EnviarAsync(cliente, "/api/recipes", Cuerpo());
            var recetaId = receta.GetProperty("id").GetGuid();
            var rendimientoBase = caso.Unidad is "g" or "ml" ? caso.Cantidad / 1000m : caso.Cantidad;
            Assert.Equal(rendimientoBase, receta.GetProperty("yield").GetDecimal());
            Assert.Equal(caso.Cantidad, receta.GetProperty("yieldQuantity").GetDecimal());
            Assert.Equal(caso.Unidad, receta.GetProperty("yieldUnitId").GetString());
            var revisionInicial = receta.GetProperty("revision").GetGuid();
            receta = await EnviarAsync(cliente, $"/api/recipes/{recetaId}", Cuerpo(revisionInicial), HttpMethod.Put);
            using (var repetido = await cliente.PutAsJsonAsync($"/api/recipes/{recetaId}", Cuerpo(revisionInicial)))
                Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
            var reabierta = (await cliente.GetFromJsonAsync<JsonElement>("/api/recipes")).EnumerateArray()
                .Single(x => x.GetProperty("id").GetGuid() == recetaId);
            Assert.Equal(caso.Cantidad, reabierta.GetProperty("yieldQuantity").GetDecimal());
            Assert.Equal(caso.Unidad, reabierta.GetProperty("yieldUnitId").GetString());
            receta = await EnviarAsync(cliente, $"/api/recipes/{recetaId}/publish", new { revision = receta.GetProperty("revision").GetGuid() });
            using (var modificada = await cliente.PutAsJsonAsync($"/api/recipes/{recetaId}", Cuerpo(receta.GetProperty("revision").GetGuid())))
                Assert.Equal(HttpStatusCode.BadRequest, modificada.StatusCode);
            var porPorciones = caso.Nombre == "Pizza por porciones";
            var plan = await EnviarAsync(cliente, "/api/production", new
            {
                establishmentId = sitioId, recipeVersionId = recetaId, quantity = caso.Plan,
                byPortions = porPorciones, unitId = porPorciones ? "" : caso.Base, date = "2026-11-05"
            });
            var cantidadBase = porPorciones ? 2m : caso.Plan;
            Assert.Equal(cantidadBase, plan.GetProperty("quantity").GetDecimal());
            foreach (var necesidad in plan.GetProperty("requirements").EnumerateArray())
                Assert.Equal(esperadas[necesidad.GetProperty("productId").GetGuid()], necesidad.GetProperty("amount").GetDecimal());
            Assert.Equal(esperadas.Count, plan.GetProperty("requirements").GetArrayLength());
            var siguiente = await EnviarAsync(cliente, $"/api/recipes/{recetaId}/versions", new { revision = receta.GetProperty("revision").GetGuid() });
            Assert.Equal(caso.Cantidad, siguiente.GetProperty("yieldQuantity").GetDecimal());
            Assert.Equal(caso.Unidad, siguiente.GetProperty("yieldUnitId").GetString());
            Assert.Equal(receta.GetProperty("yieldPortions").ToString(), siguiente.GetProperty("yieldPortions").ToString());
            await EnviarAsync(cliente, "/api/recipes/" + siguiente.GetProperty("id").GetGuid(),
                Cuerpo(siguiente.GetProperty("revision").GetGuid(), caso.Cantidad * 2), HttpMethod.Put);
            var ordenId = plan.GetProperty("id").GetGuid();
            var guardada = (await cliente.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray()
                .Single(x => x.GetProperty("id").GetGuid() == ordenId);
            Assert.Equal(recetaId, guardada.GetProperty("recipeVersionId").GetGuid());
            Assert.Equal(cantidadBase, guardada.GetProperty("quantity").GetDecimal());
            foreach (var necesidad in guardada.GetProperty("requirements").EnumerateArray())
                Assert.Equal(esperadas[necesidad.GetProperty("productId").GetGuid()], necesidad.GetProperty("amount").GetDecimal());
            evidencias.Add(new(recetaId, ordenId, rendimientoBase, caso.Cantidad, caso.Unidad, caso.Porciones, cantidadBase, esperadas));
            Console.WriteLine($"PASS QA-Recetas {caso.Nombre}: rendimiento base={rendimientoBase}; plan base={cantidadBase}; necesidades={string.Join("/", esperadas.Values)}; guardar/reabrir/editar/publicar/versionar/snapshot.");
        }
        return evidencias;
    }

    public static async Task VerificarInvalidosAsync(HttpClient cliente, HttpClient planificador, HttpClient administrador,
        HttpClient comprador, HttpClient otraEmpresa, Evidencia evidencia)
    {
        var productoId = evidencia.Necesidades.Keys.First();
        object Entrada(object[] filas, string? unidad = "kg", decimal cantidad = 1, decimal? porciones = null, Guid? producto = null) => new
        {
            name = "QA-Recetas validación", outputProductId = producto ?? productoId, yield = cantidad,
            yieldUnitId = unidad, yieldPortions = porciones, instructions = "Preparar.", ingredients = filas
        };
        var valida = new { productId = productoId, quantity = 1m, unitId = "kg" };
        async Task Invalida(object cuerpo, string campo)
        {
            using var respuesta = await cliente.PostAsJsonAsync("/api/recipes", cuerpo);
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(problema.GetProperty("errors").TryGetProperty(campo, out _), "Falta error de campo: " + campo);
        }
        await Invalida(Entrada(Array.Empty<object>()), "Ingredients");
        await Invalida(Entrada(new object[] { new { productId = Guid.Empty, quantity = 1, unitId = "kg" } }), "Ingredients[0].ProductId");
        await Invalida(Entrada(new object[] { new { productId = productoId, quantity = 0, unitId = "kg" } }), "Ingredients[0].Quantity");
        await Invalida(Entrada(new object[] { new { productId = productoId, quantity = -1, unitId = "kg" } }), "Ingredients[0].Quantity");
        await Invalida(Entrada(new object[] { new { productId = productoId, quantity = .0000001m, unitId = "kg" } }), "Ingredients[0].Quantity");
        await Invalida(Entrada(new object[] { new { productId = productoId, quantity = 1, unitId = "L" } }), "Ingredients[0].UnitId");
        await Invalida(Entrada(new object[] { valida }, "L"), "YieldUnitId");
        await Invalida(Entrada(new object[] { valida }, ""), "YieldUnitId");
        await Invalida(Entrada(new object[] { valida }, "g", .000001m), "Yield");
        await Invalida(Entrada(new object[] { valida }, porciones: 0), "YieldPortions");
        await Invalida(Entrada(new object[] { valida }, producto: Guid.NewGuid()), "OutputProductId");
        var legado = await EnviarAsync(cliente, "/api/recipes", Entrada(new object[] { valida }, null, 2));
        Assert.Equal(2m, legado.GetProperty("yield").GetDecimal());
        Assert.Equal(2m, legado.GetProperty("yieldQuantity").GetDecimal());
        Assert.Equal("kg", legado.GetProperty("yieldUnitId").GetString());
        Assert.Equal(JsonValueKind.Null, legado.GetProperty("yieldPortions").ValueKind);
        await EnviarAsync(planificador, "/api/recipes", Entrada(new object[] { valida }));
        foreach (var sinPermiso in new[] { administrador, comprador })
        {
            using var prohibida = await sinPermiso.PostAsJsonAsync("/api/recipes", Entrada(new object[] { valida }));
            Assert.Equal(HttpStatusCode.Forbidden, prohibida.StatusCode);
        }
        using (var ajena = await otraEmpresa.PostAsJsonAsync("/api/recipes", Entrada(new object[] { valida })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, ajena.StatusCode);
            var problema = await ajena.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(problema.GetProperty("errors").TryGetProperty("OutputProductId", out _));
        }
        Assert.Empty((await otraEmpresa.GetFromJsonAsync<JsonElement>("/api/recipes")).EnumerateArray());
        using (var versionAjena = await otraEmpresa.PostAsJsonAsync($"/api/recipes/{evidencia.RecetaId}/versions", new { revision = Guid.NewGuid() }))
            Assert.Equal(HttpStatusCode.NotFound, versionAjena.StatusCode);
        var produccion = (await cliente.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray().First();
        var sitio = produccion.GetProperty("establishmentId").GetGuid();
        async Task PlanInvalido(bool porPorciones, string unidad, string campo)
        {
            using var respuesta = await cliente.PostAsJsonAsync("/api/production", new
            { establishmentId = sitio, recipeVersionId = evidencia.RecetaId, quantity = 1, byPortions = porPorciones, unitId = unidad, date = "2026-11-05" });
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(problema.GetProperty("errors").TryGetProperty(campo, out _));
        }
        await PlanInvalido(true, "", "ByPortions");
        await PlanInvalido(true, "unit", "UnitId");
        await PlanInvalido(false, "", "UnitId");
        Console.WriteLine("PASS QA-Recetas: 11 entradas inválidas indexadas; legacy; planner; admin/buyer403; empresa ajena400/404 sin filtración; 3 planes inválidos.");
    }
}
