using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Persistence;
using FoodTrace.Components.Pages.Compras;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Proveedores;
using Microsoft.AspNetCore.Components.Web;
using FoodTrace.Models.Compras;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FoodTrace.Tests;

public sealed class CompraDirectaTests
{
    private static Guid Id(JsonElement element) => element.GetProperty("id").GetGuid();
    private static Guid Revision(JsonElement element) => element.GetProperty("revision").GetGuid();

    private static async Task<JsonElement> Send(HttpClient client, string path, object body, HttpMethod? method = null)
    {
        using HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(body) });
        Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Compra_directa_se_crea_envia_y_aprueba_sin_asignaciones()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient client = await app.Login("admin@test.invalid");
        JsonElement product = await Send(client, "/api/products", new { name = "Direct product", baseUnitId = "kg" });
        JsonElement supplier = await Send(client, "/api/suppliers", new { name = "Direct supplier" });
        JsonElement pack = await Send(client, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "10 kg", unitId = "kg", quantity = 10 });
        JsonElement purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier) });
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 3, allocations = Array.Empty<object>() });
        JsonElement line = Assert.Single(purchase.GetProperty("lines").EnumerateArray());
        Assert.Equal(30m, line.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(0m, line.GetProperty("grossNeed").GetDecimal());
        Assert.Empty(line.GetProperty("allocations").EnumerateArray());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        Assert.Equal(1, purchase.GetProperty("status").GetInt32());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/approve", new { revision = Revision(purchase) });
        Assert.Equal(2, purchase.GetProperty("status").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, purchase.GetProperty("decidedAt").ValueKind);
    }

    [Fact]
    public async Task Compra_directa_permite_editar_y_retirar_linea_sin_asignaciones()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        JsonElement product = await Send(admin, "/api/products", new { name = "Editable", baseUnitId = "kg" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "5 kg", unitId = "kg", quantity = 5 });
        using HttpClient client = await app.Login("buyer@test.invalid");
        JsonElement purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier) });
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 2, allocations = Array.Empty<object>() });
        JsonElement line = Assert.Single(purchase.GetProperty("lines").EnumerateArray());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines/{Id(line)}", new { revision = Revision(purchase), packCount = 4 }, HttpMethod.Put);
        line = Assert.Single(purchase.GetProperty("lines").EnumerateArray());
        Assert.Equal(20m, line.GetProperty("orderedAmount").GetDecimal());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines/{Id(line)}/remove", new { revision = Revision(purchase) });
        Assert.Empty(purchase.GetProperty("lines").EnumerateArray());
    }

    [Fact]
    public async Task Compra_planificada_mantiene_validaciones_de_cobertura_revision_y_reservas()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient client = await app.Login("admin@test.invalid");
        JsonElement site = await Send(client, "/api/establishments", new { name = "Kitchen" });
        JsonElement product = await Send(client, "/api/products", new { name = "Planned", baseUnitId = "kg" });
        JsonElement supplier = await Send(client, "/api/suppliers", new { name = "Supplier" });
        JsonElement pack = await Send(client, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "2 kg", unitId = "kg", quantity = 2 });
        JsonElement recipe = await Send(client, "/api/recipes", new { name = "Recipe", outputProductId = Id(product), yield = 1, instructions = "Cook", ingredients = new[] { new { productId = Id(product), unitId = "kg", quantity = 1 } } });
        recipe = await Send(client, $"/api/recipes/{Id(recipe)}/publish", new { revision = Revision(recipe) });
        JsonElement need = (await Send(client, "/api/production", new { establishmentId = Id(site), recipeVersionId = Id(recipe), quantity = 3, unitId = "kg", date = "2026-10-12" })).GetProperty("requirements")[0];
        Assert.Equal(3m, need.GetProperty("amount").GetDecimal());
        JsonElement purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier) });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = new[] { new { requirementId = Id(need), amount = 3, revision = Revision(need) } } })).StatusCode);
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 2, allocations = new[] { new { requirementId = Id(need), amount = 3, revision = Revision(need) } } });
        JsonElement line = Assert.Single(purchase.GetProperty("lines").EnumerateArray());
        Assert.Equal(4m, line.GetProperty("orderedAmount").GetDecimal());
        Assert.Equal(3m, line.GetProperty("grossNeed").GetDecimal());
        Assert.Equal(Id(need), Assert.Single(line.GetProperty("allocations").EnumerateArray()).GetProperty("requirementId").GetGuid());
        JsonElement stale = need;
        JsonElement other = await Send(client, "/api/purchases", new { supplierId = Id(supplier) });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/purchases/{Id(other)}/lines", new { revision = Revision(other), supplierPackId = Id(pack), packCount = 2, allocations = new[] { new { requirementId = Id(need), amount = 3, revision = Revision(need) } } })).StatusCode);
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) });
        Assert.Equal(1, purchase.GetProperty("status").GetInt32());
        JsonElement released = (await client.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == Id(need));
        Assert.Equal(3m, released.GetProperty("allocated").GetDecimal());
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/reject", new { revision = Revision(purchase), reason = "Changed plans" });
        Assert.Equal(3, purchase.GetProperty("status").GetInt32());
        released = (await client.GetFromJsonAsync<JsonElement>("/api/needs")).EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == Id(need));
        Assert.Equal(0m, released.GetProperty("allocated").GetDecimal());
    }

    [Fact]
    public async Task Cantidad_cero_rechaza_linea_directa_y_planificada()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient client = await app.Login("admin@test.invalid");
        JsonElement product = await Send(client, "/api/products", new { name = "Zero", baseUnitId = "kg" });
        JsonElement supplier = await Send(client, "/api/suppliers", new { name = "Supplier" });
        JsonElement pack = await Send(client, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });
        JsonElement purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier) });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 0, allocations = Array.Empty<object>() })).StatusCode);
    }

    [Fact]
    public async Task Presentacion_de_otro_proveedor_rechaza_linea_directa()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient client = await app.Login("admin@test.invalid");
        JsonElement product = await Send(client, "/api/products", new { name = "Other", baseUnitId = "kg" });
        JsonElement supplierA = await Send(client, "/api/suppliers", new { name = "Supplier A" });
        JsonElement supplierB = await Send(client, "/api/suppliers", new { name = "Supplier B" });
        JsonElement pack = await Send(client, "/api/packs", new { supplierId = Id(supplierA), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });
        JsonElement purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplierB) });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() })).StatusCode);
    }

    [Fact]
    public async Task Otra_empresa_no_ve_ni_edita_pedido_directo()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient admin = await app.Login("admin@test.invalid");
        using HttpClient other = await app.Login("other@test.invalid");
        JsonElement product = await Send(admin, "/api/products", new { name = "Isolated", baseUnitId = "kg" });
        JsonElement supplier = await Send(admin, "/api/suppliers", new { name = "Supplier" });
        JsonElement pack = await Send(admin, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });
        JsonElement purchase = await Send(admin, "/api/purchases", new { supplierId = Id(supplier) });
        purchase = await Send(admin, $"/api/purchases/{Id(purchase)}/lines", new { revision = Revision(purchase), supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        Assert.Empty((await other.GetFromJsonAsync<JsonElement>("/api/purchases")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/submit", new { revision = Revision(purchase) })).StatusCode);
    }

    [Fact]
    public async Task Revision_obsoleta_rechaza_escritura()
    {
        using ApiFixture app = new ApiFixture();
        await app.Seed();
        using HttpClient client = await app.Login("admin@test.invalid");
        JsonElement product = await Send(client, "/api/products", new { name = "Stale", baseUnitId = "kg" });
        JsonElement supplier = await Send(client, "/api/suppliers", new { name = "Supplier" });
        JsonElement pack = await Send(client, "/api/packs", new { supplierId = Id(supplier), productId = Id(product), name = "1 kg", unitId = "kg", quantity = 1 });
        JsonElement purchase = await Send(client, "/api/purchases", new { supplierId = Id(supplier) });
        Guid oldRevision = Revision(purchase);
        purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new { revision = oldRevision, supplierPackId = Id(pack), packCount = 1, allocations = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/submit", new { revision = oldRevision })).StatusCode);
    }

    [Fact]
    public void Modelo_detalle_directo_valida_sin_asignaciones()
    {
        Guid proveedor = Guid.NewGuid();
        Guid producto = Guid.NewGuid();
        Guid pack = Guid.NewGuid();
        CatalogosDto catalogos = new CatalogosDto
        {
            Productos = new List<ProductoDto> { new() { Id = producto, UnidadBaseId = "kg" } },
            Presentaciones = new List<PresentacionProveedorDto> { new() { Id = pack, ProveedorId = proveedor, ProductoId = producto, CantidadBase = 10m } }
        };
        PedidoCompraDto pedido = new() { Id = Guid.NewGuid(), Revision = Guid.NewGuid(), ProveedorId = proveedor };
        ModeloDetallePedido modelo = new() { PresentacionId = pack, CantidadPresentaciones = 2m };
        Assert.Empty(modelo.Validar(pedido, catalogos, new List<NecesidadIngredienteDto>()));
        AgregarDetalleCompraDto entrada = modelo.ParaEnviar(pedido.Revision);
        Assert.Empty(entrada.Asignaciones);
    }

    [Fact]
    public void Modelo_detalle_planificado_mantiene_cobertura_y_revision()
    {
        Guid proveedor = Guid.NewGuid();
        Guid producto = Guid.NewGuid();
        Guid pack = Guid.NewGuid();
        Guid necesidadId = Guid.NewGuid();
        Guid revision = Guid.NewGuid();
        CatalogosDto catalogos = new CatalogosDto
        {
            Productos = new List<ProductoDto> { new() { Id = producto, UnidadBaseId = "kg" } },
            Presentaciones = new List<PresentacionProveedorDto> { new() { Id = pack, ProveedorId = proveedor, ProductoId = producto, CantidadBase = 10m } }
        };
        PedidoCompraDto pedido = new() { Id = Guid.NewGuid(), Revision = Guid.NewGuid(), ProveedorId = proveedor };
        List<NecesidadIngredienteDto> necesidades = new()
        {
            new() { Id = necesidadId, ProductoId = producto, Revision = revision, UnidadBaseId = "kg", CantidadNecesaria = 20m, CantidadAsignada = 5m }
        };
        ModeloDetallePedido modelo = new() { PresentacionId = pack, CantidadPresentaciones = 1m };
        modelo.Asignaciones[necesidadId] = new ModeloAsignacionCompra { NecesidadId = necesidadId, Revision = revision, Cantidad = 10m };
        Assert.Empty(modelo.Validar(pedido, catalogos, necesidades));
        modelo.Asignaciones[necesidadId].Cantidad = 11m;
        Assert.NotEmpty(modelo.Validar(pedido, catalogos, necesidades));
        modelo.CantidadPresentaciones = 2m;
        modelo.Asignaciones[necesidadId].Cantidad = 10m;
        Assert.Empty(modelo.Validar(pedido, catalogos, necesidades));
        modelo.Asignaciones[necesidadId].Revision = Guid.NewGuid();
        Assert.NotEmpty(modelo.Validar(pedido, catalogos, necesidades));
    }

    [Fact]
    public async Task Pagina_habilita_enviar_con_linea_directa_y_oculta_seccion_sin_necesidades()
    {
        TransporteCompras transporte = new TransporteCompras("buyer", 0);
        PedidoCompraDto pedido = transporte.PrepararPedido();
        pedido.Detalles[0].Asignaciones = new List<AsignacionCompraDto>();
        pedido.Detalles[0].NecesidadBruta = 0m;
        using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(transporte) { BaseAddress = new Uri("https://example.invalid") });
        await cliente.IniciarSesionAsync(new IniciarSesionDto());
        using EstadoAdministracion estado = new EstadoAdministracion(cliente);
        estado.Compras.Seleccionar(pedido);
        await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
        await using AsyncServiceScope scope = servicios.CreateAsyncScope();
        HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        try
        {
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Compras>()).ToHtmlString());
                Assert.Contains("Enviar pedido", html);
                Assert.DoesNotContain("disabled", html.Substring(html.IndexOf("Enviar pedido", StringComparison.Ordinal) - 100, 100));
            });
        }
        finally
        {
            if (renderer != null) await renderer.DisposeAsync();
        }
    }
}
