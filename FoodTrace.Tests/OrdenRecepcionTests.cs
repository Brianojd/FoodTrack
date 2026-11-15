using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Domain.Enums;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FoodTrace.Tests;

public sealed class OrdenRecepcionTests
{
    private static Guid Id(JsonElement element) => element.GetProperty("id").GetGuid();
    private static Guid Revision(JsonElement element) => element.GetProperty("revision").GetGuid();

    private static async Task<JsonElement> Send(HttpClient client, string path, object body, HttpMethod? method = null)
    {
        using var response = await client.SendAsync(new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(body) });
        Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<(JsonElement Site, JsonElement Product, JsonElement Supplier, JsonElement Pack)> CrearCatalogoAsync(HttpClient client, int contenido = 5)
    {
        var site = await Send(client, "/api/establishments", new { name = "Cocina central" });
        var product = await Send(client, "/api/products", new { name = "Harina", baseUnitId = "kg" });
        var supplier = await Send(client, "/api/suppliers", new { name = "Proveedor original" });
        var pack = await Send(client, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "Bolsa 5 kg", unitId = "kg", quantity = contenido });
        return (site, product, supplier, pack);
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

    private static async Task<PedidoCompra> SembrarLegacyAsync(ApiFixture app, JsonElement product, JsonElement supplier, JsonElement pack, bool enviado)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company", app.Company.ToString())], "test"))
        };
        var db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
        var actor = await db.Users.Where(x => x.EmpresaId == app.Company && x.UserName == "admin@test.invalid").Select(x => x.Id).SingleAsync();
        var pedido = new PedidoCompra
        {
            EmpresaId = app.Company,
            ProveedorId = Id(supplier),
            Estado = EstadoPedidoCompra.Borrador,
            CreadoPorId = actor,
            FechaCreacion = DateTimeOffset.UtcNow,
            Detalles =
            [
                new DetallePedidoCompra
                {
                    EmpresaId = app.Company,
                    PresentacionProveedorId = Id(pack),
                    ProductoId = Id(product),
                    NombrePresentacion = "Bolsa 5 kg",
                    UnidadBaseId = "kg",
                    CantidadBasePresentacion = 5m,
                    CantidadPresentaciones = 3m,
                    CantidadPedida = 15m
                }
            ]
        };
        foreach (var linea in pedido.Detalles) linea.PedidoCompraId = pedido.Id;
        db.PedidosCompra.Add(pedido);
        await db.SaveChangesAsync();
        if (enviado)
        {
            pedido.Estado = EstadoPedidoCompra.Enviado;
            pedido.EnviadoPorId = actor;
            pedido.FechaEnvio = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        return pedido;
    }

    [Fact]
    public async Task Enviar_directo_3_packs_de_5kg_crea_una_orden_snapshot_e_idempotente()
    {
        using ApiFixture app = new();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        var me = await client.GetFromJsonAsync<JsonElement>("/api/me");
        var actorId = me.GetProperty("id").GetString();
        var (site, product, supplier, pack) = await CrearCatalogoAsync(client);
        var purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
        Assert.Equal(Id(site), purchase.GetProperty("establishmentId").GetGuid());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 3, allocations = Array.Empty<object>() });
        Assert.Equal(15m, purchase.GetProperty("lines")[0].GetProperty("orderedAmount").GetDecimal());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        var orderId = purchase.GetProperty("receiptOrderId").GetGuid();
        Assert.NotEqual(Guid.Empty, orderId);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/receipt-orders");
        var order = Assert.Single(list.EnumerateArray());
        Assert.Equal(orderId, Id(order));
        Assert.Equal(Id(purchase), order.GetProperty("purchaseId").GetGuid());
        Assert.Equal(app.Company, order.GetProperty("companyId").GetGuid());
        Assert.Equal("Proveedor original", order.GetProperty("supplierName").GetString());
        Assert.Equal("Cocina central", order.GetProperty("establishmentName").GetString());
        Assert.Equal(actorId, order.GetProperty("createdById").GetString());
        Assert.Equal(0, order.GetProperty("status").GetInt32());
        var detail = Assert.Single(order.GetProperty("lines").EnumerateArray());
        Assert.Equal("Harina", detail.GetProperty("productName").GetString());
        Assert.Equal("Bolsa 5 kg", detail.GetProperty("packName").GetString());
        Assert.Equal(3m, detail.GetProperty("packCount").GetDecimal());
        Assert.Equal(5m, detail.GetProperty("packBaseAmount").GetDecimal());
        Assert.Equal(15m, detail.GetProperty("expectedAmount").GetDecimal());
        Assert.Equal("kg", detail.GetProperty("baseUnitId").GetString());

        var retry = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        Assert.Equal(orderId, retry.GetProperty("receiptOrderId").GetGuid());
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/receipt-orders")).EnumerateArray());
        Assert.Equal(orderId, Id(await client.GetFromJsonAsync<JsonElement>($"/api/receipt-orders/{orderId}")));

        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company", app.Company.ToString())], "test"))
            };
            var db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
            (await db.Productos.SingleAsync(x => x.Id == Id(product))).Nombre = "Harina editada";
            (await db.Proveedores.SingleAsync(x => x.Id == Id(supplier))).Nombre = "Proveedor editado";
            (await db.Establecimientos.SingleAsync(x => x.Id == Id(site))).Nombre = "Destino editado";
            await db.SaveChangesAsync();
        }
        var readback = await client.GetFromJsonAsync<JsonElement>($"/api/receipt-orders/{orderId}");
        Assert.Equal("Proveedor original", readback.GetProperty("supplierName").GetString());
        Assert.Equal("Cocina central", readback.GetProperty("establishmentName").GetString());
        Assert.Equal("Harina", readback.GetProperty("lines")[0].GetProperty("productName").GetString());
    }

    [Fact]
    public async Task Destino_valida_empresa_se_edita_en_borrador_y_queda_bloqueado_con_orden()
    {
        using ApiFixture app = new();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        using var other = await app.Login("other@test.invalid");
        var otherSite = await Send(other, "/api/establishments", new { name = "Sitio ajeno" });
        var (site, _, supplier, pack) = await CrearCatalogoAsync(client);
        using var rejected = await client.PostAsJsonAsync("/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(otherSite) });
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);

        var purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
        using var crossEdit = await client.PutAsJsonAsync($"/api/purchases/{Id(purchase)}/destination", new { revision = Revision(purchase), establishmentId = Id(otherSite) });
        Assert.Equal(HttpStatusCode.NotFound, crossEdit.StatusCode);
        var secondSite = await Send(client, "/api/establishments", new { name = "Depósito" });
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/destination", new { revision = Revision(purchase), establishmentId = Id(secondSite) }, HttpMethod.Put);
        Assert.Equal(Id(secondSite), purchase.GetProperty("establishmentId").GetGuid());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        using var locked = await client.PutAsJsonAsync($"/api/purchases/{Id(purchase)}/destination", new { revision = Revision(purchase), establishmentId = Id(site) });
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
    }

    [Fact]
    public async Task Legacy_sin_destino_no_se_envia_y_la_reparacion_explicita_es_reintentable()
    {
        using ApiFixture app = new();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        var (site, product, supplier, pack) = await CrearCatalogoAsync(client);
        var draft = await SembrarLegacyAsync(app, product, supplier, pack, enviado: false);
        using var noDestination = await client.PostAsJsonAsync($"/api/purchases/{draft.Id}/submit", new { revision = draft.Revision });
        Assert.Equal(HttpStatusCode.BadRequest, noDestination.StatusCode);

        var sent = await SembrarLegacyAsync(app, product, supplier, pack, enviado: true);
        var saved = (await client.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == sent.Id);
        Assert.Equal(JsonValueKind.Null, saved.GetProperty("establishmentId").ValueKind);
        Assert.Equal(JsonValueKind.Null, saved.GetProperty("receiptOrderId").ValueKind);
        using var missing = await client.PostAsJsonAsync($"/api/purchases/{sent.Id}/receipt-order", new { revision = sent.Revision });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var repaired = await Send(client, $"/api/purchases/{sent.Id}/destination", new { revision = sent.Revision, establishmentId = Id(site) }, HttpMethod.Put);
        var generated = await Send(client, $"/api/purchases/{sent.Id}/receipt-order", new { revision = Revision(repaired) });
        var orderId = generated.GetProperty("receiptOrderId").GetGuid();
        Assert.NotEqual(Guid.Empty, orderId);
        var retry = await Send(client, $"/api/purchases/{sent.Id}/receipt-order", new { revision = Revision(repaired) });
        Assert.Equal(orderId, retry.GetProperty("receiptOrderId").GetGuid());
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/receipt-orders")).EnumerateArray());
    }

    [Fact]
    public async Task Lectura_de_ordenes_es_solo_admin_y_respeta_la_empresa()
    {
        using ApiFixture app = new();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        using var other = await app.Login("other@test.invalid");
        using var buyer = await app.Login("buyer@test.invalid");
        using var operatorClient = await CrearUsuarioAsync(app, "operator-receipt@test.invalid", app.Company, new[] { Roles.Operador });
        var (_, product, supplier, pack) = await CrearCatalogoAsync(client);
        var sent = await SembrarLegacyAsync(app, product, supplier, pack, enviado: true);
        var site = (await client.GetFromJsonAsync<JsonElement>("/api/catalogs")).GetProperty("establishments")[0];
        var repaired = await Send(client, $"/api/purchases/{sent.Id}/destination", new { revision = sent.Revision, establishmentId = Id(site) }, HttpMethod.Put);
        var generated = await Send(client, $"/api/purchases/{sent.Id}/receipt-order", new { revision = Revision(repaired) });
        var orderId = generated.GetProperty("receiptOrderId").GetGuid();

        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/receipt-orders")).EnumerateArray());
        Assert.Empty((await other.GetFromJsonAsync<JsonElement>("/api/receipt-orders")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/receipt-orders/{orderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/receipt-orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync($"/api/receipt-orders/{orderId}")).StatusCode);
        var operatorOrders = await operatorClient.GetFromJsonAsync<JsonElement>("/api/receipt-orders");
        Assert.Single(operatorOrders.EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await operatorClient.GetAsync($"/api/receipt-orders/{orderId}")).StatusCode);
    }

    [Fact]
    public async Task Unique_por_compra_impide_duplicados_y_el_snapshot_es_inmutable()
    {
        using ApiFixture app = new();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        var (site, _, supplier, pack) = await CrearCatalogoAsync(client);
        var purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        var orderId = purchase.GetProperty("receiptOrderId").GetGuid();

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company", app.Company.ToString())], "test"))
        };
        var db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
        var original = await db.OrdenesRecepcion.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == orderId);
        var duplicado = new OrdenRecepcion
        {
            EmpresaId = original.EmpresaId,
            PedidoCompraId = original.PedidoCompraId,
            ProveedorId = original.ProveedorId,
            NombreProveedor = original.NombreProveedor,
            EstablecimientoId = original.EstablecimientoId,
            NombreEstablecimiento = original.NombreEstablecimiento,
            FechaCreacion = original.FechaCreacion,
            Estado = original.Estado
        };
        foreach (var linea in original.Detalles)
            duplicado.Detalles.Add(new DetalleOrdenRecepcion
            {
                EmpresaId = linea.EmpresaId,
                OrdenRecepcionId = duplicado.Id,
                ProductoId = linea.ProductoId,
                NombreProducto = linea.NombreProducto,
                PresentacionProveedorId = linea.PresentacionProveedorId,
                NombrePresentacion = linea.NombrePresentacion,
                CantidadPresentaciones = linea.CantidadPresentaciones,
                CantidadBasePresentacion = linea.CantidadBasePresentacion,
                CantidadEsperada = linea.CantidadEsperada,
                UnidadBaseId = linea.UnidadBaseId
            });
        db.OrdenesRecepcion.Add(duplicado);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.OrdenesRecepcion.CountAsync(x => x.PedidoCompraId == Id(purchase)));

        var cambiada = await db.OrdenesRecepcion.SingleAsync(x => x.Id == orderId);
        cambiada.NombreProveedor = "Cambio prohibido";
        await Assert.ThrowsAsync<ExcepcionRegla>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var detalle = await db.DetallesOrdenRecepcion.SingleAsync(x => x.OrdenRecepcionId == orderId);
        detalle.CantidadEsperada = 99m;
        await Assert.ThrowsAsync<ExcepcionRegla>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Conflicto_de_unicidad_en_envio_reviene_estado_y_detalles_de_la_nueva_orden()
    {
        using ApiFixture app = new();
        await app.Seed();
        using var client = await app.Login("admin@test.invalid");
        var (site, _, supplier, pack) = await CrearCatalogoAsync(client);
        async Task<JsonElement> CrearBorradorAsync()
        {
            var creado = await Send(client, "/api/purchases", new { supplierId = Id(supplier), establishmentId = Id(site) });
            return await Send(client, $"/api/purchases/{Id(creado)}/lines", new { revision = Revision(creado), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        }
        var first = await CrearBorradorAsync();
        first = await Send(client, $"/api/purchases/{Id(first)}/submit", new { revision = Revision(first) });
        var orderId = first.GetProperty("receiptOrderId").GetGuid();
        var second = await CrearBorradorAsync();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
            await db.Database.ExecuteSqlRawAsync("UPDATE [OrdenesRecepcion] SET [PedidoCompraId] = {0} WHERE [Id] = {1}", Id(second), orderId);
        }
        using var response = await client.PostAsJsonAsync($"/api/purchases/{Id(second)}/submit", new { revision = Revision(second) });
        Assert.False(response.IsSuccessStatusCode);

        using var verification = app.Services.CreateScope();
        verification.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company", app.Company.ToString())], "test"))
        };
        var dbVerification = verification.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
        Assert.Equal(EstadoPedidoCompra.Borrador, (await dbVerification.PedidosCompra.SingleAsync(x => x.Id == Id(second))).Estado);
        Assert.Equal(1, await dbVerification.OrdenesRecepcion.CountAsync(x => x.PedidoCompraId == Id(second)));
        Assert.Equal(1, await dbVerification.DetallesOrdenRecepcion.CountAsync(x => x.OrdenRecepcionId == orderId));
    }
}
