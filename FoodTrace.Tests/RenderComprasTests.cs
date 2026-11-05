using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FoodTrace.Components.Compras;
using FoodTrace.Components.Pages.Compras;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Models.Compras;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class RenderComprasTests
{
	[Theory]
	[InlineData(new object[] { "buyer", 0 })]
	[InlineData(new object[] { "buyer", 1 })]
	[InlineData(new object[] { "buyer", 2 })]
	[InlineData(new object[] { "buyer", 3 })]
	[InlineData(new object[] { "admin", 0 })]
	[InlineData(new object[] { "admin", 1 })]
	[InlineData(new object[] { "admin", 2 })]
	[InlineData(new object[] { "admin", 3 })]
	[InlineData(new object[] { "planner", 0 })]
	[InlineData(new object[] { "planner", 1 })]
	[InlineData(new object[] { "planner", 2 })]
	[InlineData(new object[] { "planner", 3 })]
	[InlineData(new object[] { "operator", 0 })]
	[InlineData(new object[] { "operator", 1 })]
	[InlineData(new object[] { "operator", 2 })]
	[InlineData(new object[] { "operator", 3 })]
	public async Task Roles_independientes_y_estado_oficial_controlan_formularios_sin_ocultar_auditoria(string rol, int estadoPedido)
	{
		TransporteCompras transporte = new TransporteCompras(rol, estadoPedido);
		PedidoCompraDto pedido = transporte.PrepararPedido();
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(transporte)
		{
			BaseAddress = new Uri("https://example.invalid")
		});
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
				Assert.Equal(rol == "buyer", html.Contains("Crear borrador de compra", StringComparison.Ordinal));
				Assert.Equal(rol == "buyer" && estadoPedido == 0, html.Contains("Agregar detalle al borrador", StringComparison.Ordinal));
				Assert.Equal(rol == "admin" && estadoPedido == 1, html.Contains("Revisar aprobación", StringComparison.Ordinal));
				Assert.Equal(rol == "admin" && estadoPedido == 1, html.Contains("Revisar rechazo", StringComparison.Ordinal));
				Assert.Contains(EstadoCompras.NombreEstado(estadoPedido), html);
				Assert.Contains("Nombre histórico guardado", html);
				Assert.Contains("creador-sintetico", html);
				Assert.Contains("comprador-sintetico", html);
				Assert.Contains("aprobador-sintetico", html);
				Assert.Contains("Motivo persistido de prueba", html);
				Assert.Contains($"/produccion?orden={transporte.OrdenId}", html);
				Assert.DoesNotContain("PasswordHash", html);
				Assert.DoesNotContain("SecurityStamp", html);
			});
		}
		finally
		{
			if (renderer != null)
			{
				await renderer.DisposeAsync();
			}
		}
	}

	[Fact]
	public async Task Los_detalles_liberados_conservan_snapshot_y_enlaces_pero_no_acciones_de_edicion()
	{
		TransporteCompras transporte = new TransporteCompras();
		PedidoCompraDto pedido = transporte.PrepararPedido();
		pedido.Detalles[0].Asignaciones[0].Liberada = true;
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(transporte)
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		CatalogosDto catalogos = await cliente.ObtenerCatalogosAsync();
		List<NecesidadIngredienteDto> necesidades = await cliente.ObtenerNecesidadesAsync();
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<VistaPedidoCompra>(ParameterView.FromDictionary(new Dictionary<string, object?>
				{
					["Pedido"] = pedido,
					["Catalogos"] = catalogos,
					["Necesidades"] = necesidades,
					["PuedeEditar"] = true
				}))).ToHtmlString());
				Assert.Contains("Nombre histórico guardado", html);
				Assert.Contains("10 kg por presentación", html);
				Assert.Contains("Liberada · historial de solo lectura", html);
				Assert.Contains($"/produccion?orden={transporte.OrdenId}", html);
				Assert.DoesNotContain("Nombre actual distinto", html);
				Assert.DoesNotContain("Retirar detalle", html);
				Assert.DoesNotContain("Cambiar cantidad", html);
			});
		}
		finally
		{
			if (renderer != null)
			{
				await renderer.DisposeAsync();
			}
		}
	}

    [Fact]
    public async Task Vista_parcial_muestra_cantidades_agregadas_compartidas_como_no_disponibles()
    {
        TransporteCompras transporte = new TransporteCompras("planner", 0);
        PedidoCompraDto pedido = transporte.PrepararPedido();
        pedido.VistaParcial = true;
        pedido.Detalles[0].CantidadPedida = 0m;
        pedido.Detalles[0].CantidadPresentaciones = 0m;
        pedido.Detalles[0].NecesidadBruta = 0m;
        pedido.Detalles[0].CantidadBasePresentacion = 0m;
        using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(transporte)
        {
            BaseAddress = new Uri("https://example.invalid")
        });
        await cliente.IniciarSesionAsync(new IniciarSesionDto());
        using EstadoAdministracion estado = new EstadoAdministracion(cliente);
        CatalogosDto catalogos = await cliente.ObtenerCatalogosAsync();
        List<NecesidadIngredienteDto> necesidades = await cliente.ObtenerNecesidadesAsync();
        await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
        await using AsyncServiceScope scope = servicios.CreateAsyncScope();
        HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        try
        {
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<VistaPedidoCompra>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Pedido"] = pedido,
                    ["Catalogos"] = catalogos,
                    ["Necesidades"] = necesidades,
                    ["PuedeEditar"] = false
                }))).ToHtmlString());
                Assert.Contains("—", html);
                Assert.Contains("Cantidades agregadas no disponibles", html);
            });
        }
        finally
        {
            if (renderer != null)
            {
                await renderer.DisposeAsync();
            }
        }
    }

    [Theory]
    [InlineData(new object[] { "" })]
    [InlineData(new object[] { "   " })]
    public void Rechazar_exige_un_motivo_no_vacio(string motivo)
    {
		ModeloRechazoCompra modelo = new ModeloRechazoCompra
		{
			Motivo = motivo
		};
		Assert.False(Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true));
	}
}
