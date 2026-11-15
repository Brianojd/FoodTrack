using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Components.Compras;
using FoodTrace.Components.Dialogos.Compras;
using FoodTrace.Components.Pages.Compras;
using FoodTrace.Components.Pages.OrdenesRecepcion;
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
				Assert.Equal(rol == "admin", html.Contains("Crear pedido", StringComparison.Ordinal));
				Assert.Equal(rol == "admin" && estadoPedido == 0, html.Contains("Agregar detalle", StringComparison.Ordinal));
				Assert.DoesNotContain("Aprobar pedido", html);
				Assert.DoesNotContain("Rechazar pedido", html);
				if (rol != "admin")
				{
					// Con el catálogo cerrado, solo admin accede a los módulos: aviso visible y ningún dato de negocio expuesto.
					Assert.Contains("Su rol no tiene acceso a compras", html);
					Assert.DoesNotContain(EstadoCompras.NombreEstado(estadoPedido), html);
					Assert.DoesNotContain("Nombre histórico guardado", html);
					Assert.DoesNotContain("creador-sintetico", html);
					Assert.DoesNotContain("comprador-sintetico", html);
					Assert.DoesNotContain("aprobador-sintetico", html);
					Assert.DoesNotContain("Motivo persistido de prueba", html);
					Assert.DoesNotContain($"/produccion?area=planificacion&orden={transporte.OrdenId}", html);
				}
				else
				{
					Assert.Contains(EstadoCompras.NombreEstado(estadoPedido), html);
					Assert.Contains("Nombre histórico guardado", html);
					Assert.Contains("creador-sintetico", html);
					Assert.Contains("comprador-sintetico", html);
					Assert.Contains("aprobador-sintetico", html);
					Assert.Contains("Motivo persistido de prueba", html);
					Assert.Contains($"/produccion?area=planificacion&orden={transporte.OrdenId}", html);
				}
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
	public async Task Los_detalles_liberados_conservan_snapshot_y_enlaces_y_mantienen_acciones_de_edicion_en_borrador()
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
				Assert.Contains($"/produccion?area=planificacion&orden={transporte.OrdenId}", html);
				Assert.DoesNotContain("Nombre actual distinto", html);
				Assert.Contains("Cambiar cantidad", html);
				Assert.Contains("Retirar detalle", html);
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
	[InlineData(typeof(DialogoCrearPedido))]
	[InlineData(typeof(DialogoAgregarDetallePedido))]
	[InlineData(typeof(DialogoCantidadPresentaciones))]
	[InlineData(typeof(DialogoEnviarPedido))]
	[InlineData(typeof(DialogoRetirarDetallePedido))]
	public void Dialogos_de_compra_exigen_admin(Type tipo)
	{
		var dialogo = Activator.CreateInstance(tipo)!;
		var propiedad = tipo.GetProperty("RolRequerido", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(propiedad);
		Assert.Equal("admin", propiedad.GetValue(dialogo));
	}

	[Fact]
	public async Task Compra_muestra_destino_enlace_y_legacy_sin_orden_explicitamente()
	{
		TransporteCompras transporte = new("admin", 1);
		PedidoCompraDto pedido = transporte.PrepararPedido();
		using ClienteFoodTraceApi cliente = new(new HttpClient(transporte) { BaseAddress = new Uri("https://example.invalid") });
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new(cliente);
		CatalogosDto catalogos = await cliente.ObtenerCatalogosAsync();
		var sitio = Guid.NewGuid();
		catalogos.Establecimientos = [new NombreDto { Id = sitio, Nombre = "Cocina destino" }];
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				ParameterView Parametros() => ParameterView.FromDictionary(new Dictionary<string, object?> { ["Pedido"] = pedido, ["Catalogos"] = catalogos });
				string legacy = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<VistaPedidoCompra>(Parametros())).ToHtmlString());
				Assert.Contains("sin destino", legacy, StringComparison.OrdinalIgnoreCase);
				Assert.Contains("no tiene una orden de recepción", legacy, StringComparison.OrdinalIgnoreCase);
				var ordenId = Guid.NewGuid();
				pedido.EstablecimientoId = sitio;
				pedido.OrdenRecepcionId = ordenId;
				string enlazada = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<VistaPedidoCompra>(Parametros())).ToHtmlString());
				Assert.Contains("Cocina destino", enlazada);
				Assert.Contains($"/ordenes-recepcion/{ordenId}", enlazada);
			});
		}
		finally { await renderer.DisposeAsync(); }
	}

	private sealed class TransporteOrdenes : HttpMessageHandler
	{
		public bool IncluirOrden { get; set; } = true;
		public bool FallaLista { get; set; }
		public OrdenRecepcionDto Orden { get; } = new()
		{
			Id = Guid.NewGuid(), Revision = Guid.NewGuid(), PedidoCompraId = Guid.NewGuid(), ProveedorId = Guid.NewGuid(),
			NombreProveedor = "Proveedor snapshot", EstablecimientoId = Guid.NewGuid(), NombreEstablecimiento = "Cocina snapshot",
			FechaCreacion = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero), Estado = 0,
			Detalles = [new DetalleOrdenRecepcionDto
			{
				Id = Guid.NewGuid(), ProductoId = Guid.NewGuid(), NombreProducto = "Harina snapshot", PresentacionProveedorId = Guid.NewGuid(),
				NombrePresentacion = "Bolsa 5 kg", CantidadPresentaciones = 3m, CantidadBasePresentacion = 5m, CantidadEsperada = 15m, UnidadBaseId = "kg"
			}]
		};

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var ruta = request.RequestUri!.AbsolutePath;
			if (ruta == "/auth/login") return Task.FromResult(Json(new { accessToken = "token-sintetico" }));
			if (ruta == "/api/me") return Task.FromResult(Json(new UsuarioActualDto { Id = "admin-sintetico", Roles = ["admin"] }));
			if (ruta == "/api/receipt-orders") return Task.FromResult(FallaLista ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Json(IncluirOrden ? new List<OrdenRecepcionDto> { Orden } : new List<OrdenRecepcionDto>()));
			if (ruta == $"/api/receipt-orders/{Orden.Id}") return Task.FromResult(Json(Orden));
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
		}

		private static HttpResponseMessage Json(object cuerpo) => new(HttpStatusCode.OK) { Content = JsonContent.Create(cuerpo) };
	}

	private static async Task<string> RenderOrdenesAsync(TransporteOrdenes transporte, Guid? id = null)
	{
		using ClienteFoodTraceApi cliente = new(new HttpClient(transporte) { BaseAddress = new Uri("https://example.invalid") });
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new(cliente);
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			return await renderer.Dispatcher.InvokeAsync(async () =>
			{
				var parametros = id is null ? ParameterView.Empty : ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = id.Value });
				return WebUtility.HtmlDecode((await renderer.RenderComponentAsync<OrdenesRecepcion>(parametros)).ToHtmlString());
			});
		}
		finally { await renderer.DisposeAsync(); }
	}

	[Fact]
	public async Task Ordenes_muestran_lista_detalle_y_readback_admin()
	{
		TransporteOrdenes transporte = new();
		string lista = await RenderOrdenesAsync(transporte);
		Assert.Contains("Proveedor snapshot", lista);
		Assert.Contains("Cocina snapshot", lista);
		Assert.Contains("Pendiente", lista);
		Assert.DoesNotContain("Existencias actuales", lista);
		Assert.DoesNotContain("Confirmar recepción", lista);
		string detalle = await RenderOrdenesAsync(transporte, transporte.Orden.Id);
		Assert.Contains("Harina snapshot", detalle);
		Assert.Contains("Bolsa 5 kg", detalle);
		Assert.Contains("15", detalle);
		Assert.Contains("kg", detalle);
	}

	[Fact]
	public async Task Ordenes_distinguen_vacio_y_error_sin_inventar_datos()
	{
		TransporteOrdenes vacio = new() { IncluirOrden = false };
		string htmlVacio = await RenderOrdenesAsync(vacio);
		Assert.Contains("Todavía no hay órdenes de recepción", htmlVacio);
		TransporteOrdenes error = new() { FallaLista = true };
		string htmlError = await RenderOrdenesAsync(error);
		Assert.Contains("no pudo completar la solicitud", htmlError);
		Assert.DoesNotContain("Proveedor snapshot", htmlError);
	}

}



