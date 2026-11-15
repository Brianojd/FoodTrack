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
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task Planner_ya_no_escribe_recetas_ni_produccion_solo_admin()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Beef", baseUnitId = "kg" });

        using HttpClient planner = await CrearUsuarioAsync(app, "planner-s1@test.invalid", app.Company, new[] { "planner" });

        object recipeBody = new { name = "Burger", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } };
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.PostAsJsonAsync("/api/recipes", recipeBody)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.PutAsJsonAsync($"/api/recipes/{Guid.NewGuid()}", recipeBody)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.PostAsJsonAsync($"/api/recipes/{Guid.NewGuid()}/publish", new { revision = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.PostAsJsonAsync($"/api/recipes/{Guid.NewGuid()}/versions", new { revision = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.PostAsJsonAsync("/api/production", new { establishmentId = Id(site), recipeVersionId = Guid.NewGuid(), quantity = 1, unitId = "kg", date = "2026-10-12" })).StatusCode);

        JsonElement recipe = await Send(admin, "/api/recipes", recipeBody);
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });
        JsonElement order = await Send(admin, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" });
        Assert.Single(order.GetProperty("requirements").EnumerateArray());
    }

    [Fact]
    public async Task Buyer_ya_no_escribe_compras_solo_admin()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });

        using HttpClient buyer = await app.Login("buyer@test.invalid");
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsJsonAsync("/api/purchases", new { supplierId = Id(supplier) })).StatusCode);

        JsonElement purchase = await Send(admin, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) })).StatusCode);

        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        Assert.Equal(1, purchase.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Aprobacion_y_rechazo_de_compras_ya_no_existen()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });

        JsonElement purchase = await Send(admin, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        Assert.Equal(1, purchase.GetProperty("status").GetInt32());
        Assert.Equal(JsonValueKind.Null, purchase.GetProperty("decidedAt").ValueKind);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/approve", new { revision = Revision(purchase) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/reject", new { revision = Revision(purchase), reason = "Circuito eliminado" })).StatusCode);
    }

    [Fact]
    public async Task Los_GET_listan_toda_la_empresa_sin_vista_por_propietario()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Recipe", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });
        await Send(admin, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" });
        await Send(admin, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });

        // Un segundo administrador ve exactamente lo mismo que el primero: sin alcance por propietario.
        using HttpClient otroAdmin = await CrearUsuarioAsync(app, "admin-par@test.invalid", app.Company, new[] { "admin" });

        Assert.Equal(1, (await otroAdmin.GetFromJsonAsync<JsonElement>("/api/production")).GetArrayLength());
        Assert.Equal(1, (await otroAdmin.GetFromJsonAsync<JsonElement>("/api/needs")).GetArrayLength());
        JsonElement compras = await otroAdmin.GetFromJsonAsync<JsonElement>("/api/purchases");
        Assert.Equal(1, compras.GetArrayLength());
        Assert.Throws<KeyNotFoundException>(() => compras[0].GetProperty("partialView"));

        // Los roles históricos (planner) ya no tienen acceso de módulo en la Web.
        using HttpClient planner = await CrearUsuarioAsync(app, "planner-s5@test.invalid", app.Company, new[] { "planner" });
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.GetAsync("/api/production")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.GetAsync("/api/needs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.GetAsync("/api/purchases")).StatusCode);
    }

    [Fact]
    public async Task Editar_membresia_conserva_roles_historicos_y_rechaza_los_no_ofrecidos()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        using HttpClient historico = await CrearUsuarioAsync(app, "roles-historicos@test.invalid", app.Company, new[] { "admin", "planner", "buyer", "approver" });
        JsonElement[] usuarios = (await admin.GetFromJsonAsync<JsonElement>("/api/users")).EnumerateArray().ToArray();
        string id = usuarios.Single(x => x.GetProperty("email").GetString() == "roles-historicos@test.invalid").GetProperty("id").GetString()!;

        // Cambiar habilitación y roles ofrecidos conserva todas las membresías históricas.
        JsonElement deshabilitado = await Send(admin, $"/api/users/{id}", new { enabled = false, roles = new[] { "operator" } }, HttpMethod.Put);
        Assert.False(deshabilitado.GetProperty("enabled").GetBoolean());
        AssertRoles(deshabilitado, "operator", "planner", "buyer", "approver");
        JsonElement actualizado = await Send(admin, $"/api/users/{id}", new { enabled = true, roles = new[] { "admin" } }, HttpMethod.Put);
        Assert.True(actualizado.GetProperty("enabled").GetBoolean());
        AssertRoles(actualizado, "admin", "planner", "buyer", "approver");

        using IServiceScope scope = app.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        AppUser persisted = await users.FindByIdAsync(id) ?? throw new InvalidOperationException("User not found.");
        Assert.Equal(new[] { "admin", "approver", "buyer", "planner" }, (await users.GetRolesAsync(persisted)).Order().ToArray());

        // Ningún rol histórico puede asignarse de nuevo mediante el catálogo editable.
        foreach (string role in new[] { "planner", "buyer", "approver" })
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/users/{id}", new { enabled = true, roles = new[] { role } })).StatusCode);

        static void AssertRoles(JsonElement user, params string[] expected)
            => Assert.Equal(expected.Order(), user.GetProperty("roles").EnumerateArray().Select(x => x.GetString()!).Order());
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
    public async Task Admin_de_otra_empresa_no_ve_registros_ajenos()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Recipe", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });

        // Admin de otra empresa con acceso autorizado: el aislamiento lo garantiza el filtro global de empresa.
        using HttpClient otroAdmin = await CrearUsuarioAsync(app, "admin-otra@test.invalid", app.OtherCompany, new[] { "admin" });

        JsonElement order = await Send(admin, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" });
        Guid needId = order.GetProperty("requirements")[0].GetProperty("id").GetGuid();

        Assert.Empty((await otroAdmin.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray());
        Assert.Empty((await otroAdmin.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray());
        Assert.Empty((await otroAdmin.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
        // Escribir sobre un pedido inexistente en su empresa devuelve 404 del servicio, sin exponer datos ajenos.
        Assert.Equal(HttpStatusCode.NotFound, (await otroAdmin.PostAsJsonAsync($"/api/purchases/{Guid.NewGuid()}/lines", new { revision = Guid.NewGuid(), supplierPackId = Guid.NewGuid(), packCount = 1, allocations = new[] { new { requirementId = needId, amount = 1, revision = Guid.NewGuid() } } })).StatusCode);
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

        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/recipes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/production")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/needs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/purchases")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.PostAsJsonAsync("/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.PostAsJsonAsync("/api/purchases", new { supplierId = Guid.NewGuid() })).StatusCode);
    }

    [Fact]
    public async Task Roles_limitan_los_GET_por_modulo()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Recipe", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });
        await Send(admin, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" });
        await Send(admin, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });

        using HttpClient buyer = await app.Login("buyer@test.invalid");
        using HttpClient planner = await CrearUsuarioAsync(app, "planner-get@test.invalid", app.Company, new[] { "planner" });
        using HttpClient operador = await CrearUsuarioAsync(app, "operator-get@test.invalid", app.Company, new[] { "operator" });
        using HttpClient anonymous = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/recipes")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/production")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/needs")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/purchases")).StatusCode);

        // Con el catálogo cerrado, solo admin accede a los módulos en la Web.
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/recipes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/production")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/needs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/purchases")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await planner.GetAsync("/api/recipes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.GetAsync("/api/production")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.GetAsync("/api/needs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.GetAsync("/api/purchases")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/recipes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/production")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/needs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/purchases")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/recipes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/production")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/needs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/purchases")).StatusCode);
    }

    [Fact]
    public async Task GET_multiempresa_aisla_por_empresa_para_roles_autorizados()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");

        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement recipe = await Send(admin, "/api/recipes", new { name = "Recipe", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(admin, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });
        await Send(admin, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 1, unitId = "kg", date = "2026-10-12" });
        await Send(admin, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });

        using HttpClient adminB = await CrearUsuarioAsync(app, "admin-b@test.invalid", app.OtherCompany, new[] { "admin" });

        Assert.Empty((await adminB.GetFromJsonAsync<JsonElement>("/api/production")).EnumerateArray());
        Assert.Empty((await adminB.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray());
        Assert.Empty((await adminB.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
    }

    [Fact]
    public async Task Admin_puede_crear_agregar_y_enviar_compra_y_planificador_no_puede_crear()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await CrearUsuarioAsync(app, "admin-only@test.invalid", app.Company, new[] { "admin" });
        JsonElement site = await Send(admin, "/api/establishments", new { name = "Kitchen" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement product = await Send(admin, "/api/products", new { name = "Item", baseUnitId = "kg" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });

        JsonElement purchase = await Send(admin, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        Assert.Equal(1, purchase.GetProperty("status").GetInt32());

        using HttpClient planner = await CrearUsuarioAsync(app, "planner-write@test.invalid", app.Company, new[] { "planner" });
        Assert.Equal(HttpStatusCode.Forbidden, (await planner.PostAsJsonAsync("/api/purchases", new { supplierId = Id(supplier) })).StatusCode);
    }
}
