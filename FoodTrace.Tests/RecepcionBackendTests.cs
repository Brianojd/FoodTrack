using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FoodTrace.Tests;

public sealed class RecepcionBackendTests
{
    private static Guid Id(JsonElement value) => value.GetProperty("id").GetGuid();
    private static Guid Revision(JsonElement value) => value.GetProperty("revision").GetGuid();

    private static async Task<JsonElement> PostOk(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<(JsonElement Purchase, JsonElement Order)> CreateOrderAsync(HttpClient client, int packs = 3, int packContent = 5)
    {
        var site = await PostOk(client, "/api/establishments", new { name = "Destino recepción" });
        var product = await PostOk(client, "/api/products", new { name = "Producto recepción", baseUnitId = "kg" });
        var supplier = await PostOk(client, "/api/suppliers", new { name = "Proveedor recepción" });
        var pack = await PostOk(client, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "Caja recepción", unitId = "kg", quantity = packContent });
        var purchase = await PostOk(client, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
        purchase = await PostOk(client, $"/api/purchases/{Id(purchase)}/lines", new
        {
            revision = Revision(purchase), supplierPackId = Id(pack), packCount = packs, allocations = Array.Empty<object>()
        });
        purchase = await PostOk(client, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        var order = await client.GetFromJsonAsync<JsonElement>($"/api/receipt-orders/{purchase.GetProperty("receiptOrderId").GetGuid()}");
        return (purchase, order);
    }

    [Fact]
    public async Task Confirma_faltante_cierra_orden_e_ingresa_solo_lo_recibido_e_idempotente()
    {
        using var app = new ApiFixture();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        var (_, order) = await CreateOrderAsync(client);
        var orderId = Id(order);
        var line = order.GetProperty("lines")[0];
        var body = new
        {
            revision = Revision(order),
            details = new[] { new { receiptOrderLineId = Id(line), receivedPackCount = 2 } },
            observation = "Faltó una caja"
        };

        var receipt = await PostOk(client, $"/api/receipt-orders/{orderId}/receipt", body);
        var detail = receipt.GetProperty("details")[0];
        Assert.Equal(15m, detail.GetProperty("expectedAmount").GetDecimal());
        Assert.Equal(10m, detail.GetProperty("receivedAmount").GetDecimal());
        Assert.Equal(5m, detail.GetProperty("missingAmount").GetDecimal());
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/receipt-orders")).EnumerateArray());

        var retry = await PostOk(client, $"/api/receipt-orders/{orderId}/receipt", body);
        Assert.Equal(Id(receipt), Id(retry));
        var saved = await client.GetFromJsonAsync<JsonElement>($"/api/receipt-orders/{orderId}/receipt");
        Assert.Equal(Id(receipt), Id(saved));
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company", app.Company.ToString())], "test"))
        };
        var db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
        Assert.Equal(1, await db.IngresosInventario.CountAsync());
        Assert.Equal(10m, await db.IngresosInventario.Select(x => x.Cantidad).SingleAsync());
    }

    [Fact]
    public async Task Permite_recepcion_cero_sin_crear_ingreso_y_rechaza_datos_incompletos()
    {
        using var app = new ApiFixture();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        var (_, order) = await CreateOrderAsync(client, packs: 1);
        var orderId = Id(order);
        var lineId = Id(order.GetProperty("lines")[0]);
        var revision = Revision(order);

        using var negativo = await client.PostAsJsonAsync($"/api/receipt-orders/{orderId}/receipt", new
        {
            revision, details = new[] { new { receiptOrderLineId = lineId, receivedPackCount = -1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, negativo.StatusCode);
        using var omitido = await client.PostAsJsonAsync($"/api/receipt-orders/{orderId}/receipt", new { revision, details = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, omitido.StatusCode);

        var receipt = await PostOk(client, $"/api/receipt-orders/{orderId}/receipt", new
        {
            revision, details = new[] { new { receiptOrderLineId = lineId, receivedPackCount = 0 } }
        });
        Assert.Equal(0m, receipt.GetProperty("details")[0].GetProperty("receivedAmount").GetDecimal());
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company", app.Company.ToString())], "test"))
        };
        var db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
        Assert.Empty(await db.IngresosInventario.ToListAsync());
    }

    [Fact]
    public async Task Solo_admin_consulta_diferencias_y_operator_puede_confirmar()
    {
        using var app = new ApiFixture();
        await app.Seed();
        using var admin = await app.Login("admin@test.invalid");
        using var operatorClient = await CrearUsuarioAsync(app, "operator-reception@test.invalid", app.Company, [Roles.Operador]);
        var (_, order) = await CreateOrderAsync(admin, packs: 2);
        var lineId = Id(order.GetProperty("lines")[0]);
        var receipt = await PostOk(operatorClient, $"/api/receipt-orders/{Id(order)}/receipt", new
        {
            revision = Revision(order), details = new[] { new { receiptOrderLineId = lineId, receivedPackCount = 1 } }
        });
        Assert.NotEqual(Guid.Empty, Id(receipt));
        using var differences = await admin.GetAsync("/api/receipt-differences");
        Assert.True(differences.IsSuccessStatusCode, await differences.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await operatorClient.GetAsync("/api/receipt-differences")).StatusCode);
    }

    private static async Task<HttpClient> CrearUsuarioAsync(ApiFixture app, string email, Guid empresaId, string[] roles)
    {
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                InicializadorIdentidad.ComprobarResultado(await roleManager.CreateAsync(new IdentityRole(role)));
        var user = new AppUser { UserName = email, Email = email, EmpresaId = empresaId };
        InicializadorIdentidad.ComprobarResultado(await users.CreateAsync(user, ApiFixture.Password));
        InicializadorIdentidad.ComprobarResultado(await users.AddToRolesAsync(user, roles));
        return await app.Login(email);
    }
}
