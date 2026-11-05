using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FoodTrace.Tests;

public sealed class RolesPlannerTests
{
    private static Guid Id(JsonElement element) => element.GetProperty("id").GetGuid();
    private static Guid Revision(JsonElement element) => element.GetProperty("revision").GetGuid();

    private static async Task<JsonElement> Send(HttpClient client, string path, object body, HttpMethod? method = null)
    {
        using HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(body) });
        Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<HttpClient> CrearUsuarioAsync(ApiFixture app, string email, Guid empresaId, string[] roles)
    {
        using IServiceScope scope = app.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (string rol in roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                InicializadorIdentidad.ComprobarResultado(await roleManager.CreateAsync(new IdentityRole(rol)));
        }
        AppUser user = new()
        {
            UserName = email,
            Email = email,
            EmpresaId = empresaId
        };
        InicializadorIdentidad.ComprobarResultado(await users.CreateAsync(user, ApiFixture.Password));
        InicializadorIdentidad.ComprobarResultado(await users.AddToRolesAsync(user, roles));
        return await app.Login(email);
    }

    [Fact]
    public async Task Dos_planificadores_ven_solo_sus_necesidades_y_sus_asignaciones_en_pedidos_compartidos()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Beef", baseUnitId = "kg" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "10 kg", unitId = "kg", quantity = 10 });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Burger", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });

        using HttpClient plannerA = await CrearUsuarioAsync(app, "planner-a@test.invalid", app.Company, new[] { "planner" });
        using HttpClient plannerB = await CrearUsuarioAsync(app, "planner-b@test.invalid", app.Company, new[] { "planner" });

        JsonElement orderA = await Send(plannerA, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 5, unitId = "kg", date = "2026-10-12" });
        JsonElement orderB = await Send(plannerB, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 3, unitId = "kg", date = "2026-10-12" });
        JsonElement needA = orderA.GetProperty("requirements")[0];
        JsonElement needB = orderB.GetProperty("requirements")[0];

        using HttpClient buyer = await app.Login("buyer@test.invalid");
        JsonElement purchase = await Send(buyer, "/api/purchases", new { supplierId = Id(supplier) });
        purchase = await Send(buyer, $"/api/purchases/{Id(purchase)}/lines", new
        {
            revision = Revision(purchase),
            supplierPackId = Id(pack),
            packCount = 2,
            allocations = new[]
            {
                new { requirementId = Id(needA), amount = 5, revision = Revision(needA) },
                new { requirementId = Id(needB), amount = 3, revision = Revision(needB) }
            }
        });

        JsonElement[] prodA = (await plannerA.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray().ToArray();
        JsonElement[] needsA = (await plannerA.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray().ToArray();
        JsonElement[] purchasesA = (await plannerA.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray().ToArray();

        Assert.Single(prodA);
        Assert.Equal(Id(orderA), prodA[0].GetProperty("id").GetGuid());
        Assert.Single(needsA);
        Assert.Equal(Id(needA), needsA[0].GetProperty("id").GetGuid());
        Assert.Single(purchasesA);
        JsonElement lineA = Assert.Single(purchasesA[0].GetProperty("lines").EnumerateArray());
        Assert.True(purchasesA[0].GetProperty("partialView").GetBoolean());
        Assert.Single(lineA.GetProperty("allocations").EnumerateArray());
        Assert.Equal(Id(needA), lineA.GetProperty("allocations")[0].GetProperty("requirementId").GetGuid());
        Assert.Equal(5m, lineA.GetProperty("allocations")[0].GetProperty("amount").GetDecimal());
        Assert.Equal(0m, lineA.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(0m, lineA.GetProperty("grossNeed").GetDecimal());
        Assert.Equal(0m, lineA.GetProperty("packCount").GetDecimal());
        Assert.Equal(0m, lineA.GetProperty("packBaseAmount").GetDecimal());

        JsonElement[] prodB = (await plannerB.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray().ToArray();
        JsonElement[] needsB = (await plannerB.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray().ToArray();
        JsonElement[] purchasesB = (await plannerB.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray().ToArray();

        Assert.Single(prodB);
        Assert.Equal(Id(orderB), prodB[0].GetProperty("id").GetGuid());
        Assert.Single(needsB);
        Assert.Equal(Id(needB), needsB[0].GetProperty("id").GetGuid());
        Assert.Single(purchasesB);
        JsonElement lineB = Assert.Single(purchasesB[0].GetProperty("lines").EnumerateArray());
        Assert.True(purchasesB[0].GetProperty("partialView").GetBoolean());
        Assert.Single(lineB.GetProperty("allocations").EnumerateArray());
        Assert.Equal(Id(needB), lineB.GetProperty("allocations")[0].GetProperty("requirementId").GetGuid());
        Assert.Equal(3m, lineB.GetProperty("allocations")[0].GetProperty("amount").GetDecimal());
        Assert.Equal(0m, lineB.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(0m, lineB.GetProperty("grossNeed").GetDecimal());
        Assert.Equal(0m, lineB.GetProperty("packCount").GetDecimal());
        Assert.Equal(0m, lineB.GetProperty("packBaseAmount").GetDecimal());

        JsonElement[] adminPurchases = (await admin.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray().ToArray();
        Assert.Single(adminPurchases);
        Assert.False(adminPurchases[0].GetProperty("partialView").GetBoolean());
        JsonElement adminLine = Assert.Single(adminPurchases[0].GetProperty("lines").EnumerateArray());
        Assert.Equal(2, adminLine.GetProperty("allocations").EnumerateArray().Count());
        Assert.Equal(20m, adminLine.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(8m, adminLine.GetProperty("grossNeed").GetDecimal());
        Assert.Equal(2m, adminLine.GetProperty("packCount").GetDecimal());
        Assert.Equal(10m, adminLine.GetProperty("packBaseAmount").GetDecimal());

        JsonElement[] buyerPurchases = (await buyer.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray().ToArray();
        Assert.Single(buyerPurchases);
        Assert.False(buyerPurchases[0].GetProperty("partialView").GetBoolean());
        JsonElement buyerLine = Assert.Single(buyerPurchases[0].GetProperty("lines").EnumerateArray());
        Assert.Equal(20m, buyerLine.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(8m, buyerLine.GetProperty("grossNeed").GetDecimal());
        Assert.Equal(2m, buyerLine.GetProperty("packCount").GetDecimal());
        Assert.Equal(10m, buyerLine.GetProperty("packBaseAmount").GetDecimal());
    }

    [Fact]
    public async Task Aprobador_no_puede_aprobar_y_admin_si()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });

        using HttpClient approver = await CrearUsuarioAsync(app, "approver@test.invalid", app.Company, new[] { "approver" });
        using HttpClient buyer = await app.Login("buyer@test.invalid");
        JsonElement purchase = await Send(buyer, "/api/purchases", new { supplierId = Id(supplier) });
        purchase = await Send(buyer, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        purchase = await Send(buyer, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });

        Assert.Equal(HttpStatusCode.Forbidden, (await approver.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/approve", new { revision = Revision(purchase) })).StatusCode);
        JsonElement approved = await Send(admin, $"/api/purchases/{Id(purchase)}/approve", new { revision = Revision(purchase) });
        Assert.Equal(2, approved.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Api_me_no_lista_aprobador_y_si_lista_operador()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient approver = await CrearUsuarioAsync(app, "approver-me@test.invalid", app.Company, new[] { "approver" });
        using HttpClient operatorUser = await CrearUsuarioAsync(app, "operator-me@test.invalid", app.Company, new[] { "operator" });

        JsonElement approverIdentity = await approver.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.DoesNotContain("approver", approverIdentity.GetProperty("roles").EnumerateArray().Select(x => x.GetString()));

        JsonElement operatorIdentity = await operatorUser.GetFromJsonAsync<JsonElement>("/api/me");
        string[] operatorRoles = operatorIdentity.GetProperty("roles").EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Contains("operator", operatorRoles);
        Assert.DoesNotContain("approver", operatorRoles);
    }

    [Fact]
    public async Task Compra_directa_es_invisible_para_planificador()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });

        using HttpClient buyer = await app.Login("buyer@test.invalid");
        using HttpClient planner = await CrearUsuarioAsync(app, "planner-direct@test.invalid", app.Company, new[] { "planner" });

        JsonElement purchase = await Send(buyer, "/api/purchases", new { supplierId = Id(supplier) });
        purchase = await Send(buyer, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });

        Assert.Single((await buyer.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
        Assert.Single((await admin.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
        Assert.Empty((await planner.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
    }

    [Fact]
    public async Task Planificador_de_otra_empresa_no_ve_registros_ajenos()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Recipe", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });

        using HttpClient otherPlanner = await CrearUsuarioAsync(app, "planner-other@test.invalid", app.OtherCompany, new[] { "planner" });

        JsonElement order = await Send(admin, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" });
        Guid needId = order.GetProperty("requirements")[0].GetProperty("id").GetGuid();

        Assert.Empty((await otherPlanner.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray());
        Assert.Empty((await otherPlanner.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray());
        Assert.Empty((await otherPlanner.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await otherPlanner.PostAsJsonAsync($"/api/purchases/{Guid.NewGuid()}/lines", new { revision = Guid.NewGuid(), supplierPackId = Guid.NewGuid(), packCount = 1, allocations = new[] { new { requirementId = needId, amount = 1, revision = Guid.NewGuid() } } })).StatusCode);
    }

    [Fact]
    public async Task Operador_tiene_vistas_vacias_y_escrituras_rechazadas()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Recipe", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });

        using HttpClient operador = await CrearUsuarioAsync(app, "operator@test.invalid", app.Company, new[] { "operator" });

        Assert.Empty((await operador.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray());
        Assert.Empty((await operador.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray());
        Assert.Empty((await operador.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.PostAsJsonAsync("/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.PostAsJsonAsync("/api/purchases", new { supplierId = Guid.NewGuid() })).StatusCode);
    }

    [Fact]
    public async Task Linea_exclusiva_de_planificador_mantiene_los_agregados_y_linea_compartida_los_neutraliza()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Beef", baseUnitId = "kg" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "10 kg", unitId = "kg", quantity = 10 });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Burger", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });

        using HttpClient plannerA = await CrearUsuarioAsync(app, "planner-exclusive-a@test.invalid", app.Company, new[] { "planner" });
        using HttpClient plannerB = await CrearUsuarioAsync(app, "planner-exclusive-b@test.invalid", app.Company, new[] { "planner" });

        JsonElement orderA = await Send(plannerA, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 5, unitId = "kg", date = "2026-10-12" });
        JsonElement orderA2 = await Send(plannerA, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 7, unitId = "kg", date = "2026-10-13" });
        JsonElement orderB = await Send(plannerB, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 3, unitId = "kg", date = "2026-10-12" });
        JsonElement needA = orderA.GetProperty("requirements")[0];
        JsonElement needA2 = orderA2.GetProperty("requirements")[0];
        JsonElement needB = orderB.GetProperty("requirements")[0];

        using HttpClient buyer = await app.Login("buyer@test.invalid");
        JsonElement sharedPurchase = await Send(buyer, "/api/purchases", new { supplierId = Id(supplier) });
        sharedPurchase = await Send(buyer, $"/api/purchases/{Id(sharedPurchase)}/lines", new
        {
            revision = Revision(sharedPurchase),
            supplierPackId = Id(pack),
            packCount = 2,
            allocations = new[]
            {
                new { requirementId = Id(needA), amount = 5, revision = Revision(needA) },
                new { requirementId = Id(needB), amount = 3, revision = Revision(needB) }
            }
        });
        JsonElement exclusivePurchase = await Send(buyer, "/api/purchases", new { supplierId = Id(supplier) });
        exclusivePurchase = await Send(buyer, $"/api/purchases/{Id(exclusivePurchase)}/lines", new
        {
            revision = Revision(exclusivePurchase),
            supplierPackId = Id(pack),
            packCount = 1,
            allocations = new[] { new { requirementId = Id(needA2), amount = 7, revision = Revision(needA2) } }
        });

        JsonElement[] purchasesA = (await plannerA.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray().ToArray();
        Assert.Equal(2, purchasesA.Length);
        JsonElement exclusiveLine = Assert.Single(purchasesA.Single(x => x.GetProperty("lines")[0].GetProperty("allocations").EnumerateArray().Count() == 1 && x.GetProperty("lines")[0].GetProperty("allocations")[0].GetProperty("requirementId").GetGuid() == Id(needA2)).GetProperty("lines").EnumerateArray());
        Assert.Equal(10m, exclusiveLine.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(7m, exclusiveLine.GetProperty("grossNeed").GetDecimal());
        Assert.Equal(1m, exclusiveLine.GetProperty("packCount").GetDecimal());
        Assert.Equal(10m, exclusiveLine.GetProperty("packBaseAmount").GetDecimal());

        JsonElement sharedLine = Assert.Single(purchasesA.Single(x => x.GetProperty("lines")[0].GetProperty("allocations").EnumerateArray().Count() == 1 && x.GetProperty("lines")[0].GetProperty("allocations")[0].GetProperty("requirementId").GetGuid() == Id(needA)).GetProperty("lines").EnumerateArray());
        Assert.Equal(0m, sharedLine.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(0m, sharedLine.GetProperty("grossNeed").GetDecimal());
        Assert.Equal(0m, sharedLine.GetProperty("packCount").GetDecimal());
        Assert.Equal(0m, sharedLine.GetProperty("packBaseAmount").GetDecimal());

        JsonElement[] purchasesB = (await plannerB.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray().ToArray();
        Assert.Single(purchasesB);
        JsonElement bLine = Assert.Single(purchasesB[0].GetProperty("lines").EnumerateArray());
        Assert.Equal(0m, bLine.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(3m, bLine.GetProperty("allocations")[0].GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Admin_puede_crear_agregar_y_enviar_compra_y_planificador_no_puede_crear()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await CrearUsuarioAsync(app, "admin-only@test.invalid", app.Company, new[] { "admin" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });

        JsonElement purchase = await Send(admin, "/api/purchases", new { supplierId = Id(supplier) });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        Assert.Equal(1, purchase.GetProperty("status").GetInt32());

        using HttpClient planner = await CrearUsuarioAsync(app, "planner-write@test.invalid", app.Company, new[] { "planner" });
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.PostAsJsonAsync("/api/purchases", new { supplierId = Id(supplier) })).StatusCode);
    }
}
