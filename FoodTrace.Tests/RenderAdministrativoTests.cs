using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Components.Pages.Productos;
using FoodTrace.Components.Shared;
using FoodTrace.Components.Usuarios;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.UnidadesMedida;
using FoodTrace.Services.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class RenderAdministrativoTests
{
	private sealed class JsSinNavegador : IJSRuntime
	{
		// El doble sin navegador devuelve intencionadamente default incluso para referencias.
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			return ValueTask.FromResult<TValue>(default!);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			return ValueTask.FromResult<TValue>(default!);
		}
	}

	private sealed class NavegacionPrueba : NavigationManager
	{
		public NavegacionPrueba()
		{
			Initialize("https://example.invalid/", "https://example.invalid/productos");
		}

		protected override void NavigateToCore(string uri, bool forceLoad)
		{
		}
	}

	private sealed class Transporte(string rol) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// HttpClient entrega al transporte una URI absoluta.
			string absolutePath = request.RequestUri!.AbsolutePath;
			if (1 == 0)
			{
			}
			object obj;
			if (!(absolutePath == "/auth/login"))
			{
				if (absolutePath == "/api/me")
				{
					UsuarioActualDto usuarioActualDto = new UsuarioActualDto();
					usuarioActualDto.Id = "cuenta";
					usuarioActualDto.Roles = new string[1] { rol };
					obj = usuarioActualDto;
				}
				else
				{
					CatalogosDto catalogosDto = new CatalogosDto();
					int num = 1;
					List<ProductoDto> list = new List<ProductoDto>(num);
					CollectionsMarshal.SetCount(list, num);
					CollectionsMarshal.AsSpan(list)[0] = new ProductoDto
					{
						Id = Guid.NewGuid(),
						Nombre = "Producto de prueba",
						UnidadBaseId = "kg"
					};
					catalogosDto.Productos = list;
					num = 1;
					List<UnidadMedidaDto> list2 = new List<UnidadMedidaDto>(num);
					CollectionsMarshal.SetCount(list2, num);
					CollectionsMarshal.AsSpan(list2)[0] = new UnidadMedidaDto
					{
						Id = "kg",
						Dimension = "mass",
						FactorConversion = 1m
					};
					catalogosDto.UnidadesMedida = list2;
					obj = catalogosDto;
				}
			}
			else
			{
				obj = new
				{
					accessToken = "token-de-prueba"
				};
			}
			if (1 == 0)
			{
			}
			object cuerpo = obj;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = JsonContent.Create(cuerpo)
			});
		}
	}

	[Theory]
	[InlineData(new object[] { "admin", true })]
	[InlineData(new object[] { "buyer", false })]
	public async Task Productos_muestra_datos_reales_del_cliente_y_solo_admin_puede_crear(string rol, bool puedeCrear)
	{
		await using ServiceProvider servicios = CrearServicios(rol);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		ClienteFoodTraceApi cliente = scope.ServiceProvider.GetRequiredService<ClienteFoodTraceApi>();
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Productos>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["EnContenedor"] = true }))).ToHtmlString());
				Assert.Contains("Producto de prueba", html);
				Assert.Contains("Unidad base", html);
				Assert.Equal(puedeCrear, html.Contains("Crear producto", StringComparison.Ordinal));
				Assert.DoesNotContain("role=\"alert\"", html);
				Assert.DoesNotContain("unidad.Id", html);
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
	public async Task Los_roles_tienen_etiquetas_espanolas_y_un_error_vacio_no_genera_alerta()
	{
		await using ServiceProvider servicios = CrearServicios("admin");
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<SelectorRoles>()).ToHtmlString());
				Assert.Contains("Administrador", html);
				Assert.Contains("Comprador", html);
				Assert.Contains("Planificador", html);
				Assert.Contains("Aprobador", html);
				Assert.DoesNotContain("rol.Nombre", html);
				Assert.DoesNotContain("role=\"alert\"", (await renderer.RenderComponentAsync<MensajeError>()).ToHtmlString());
				HtmlRootComponent error = await renderer.RenderComponentAsync<MensajeError>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Mensaje"] = "Error de prueba" }));
				Assert.Contains("role=\"alert\"", error.ToHtmlString());
				Assert.Contains("Error de prueba", error.ToHtmlString());
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

	private static ServiceProvider CrearServicios(string rol)
	{
		IServiceCollection servicios = new ServiceCollection().AddLogging();
		servicios.AddSingleton<IJSRuntime, JsSinNavegador>();
		servicios.AddSingleton<NavigationManager, NavegacionPrueba>();
		servicios.AddMudServices();
		servicios.AddHttpClient("FoodTrace", (HttpClient cliente) =>
		{
			cliente.BaseAddress = new Uri("https://example.invalid");
		}).ConfigurePrimaryHttpMessageHandler(() => new Transporte(rol));
		servicios.AgregarClienteFoodTrace();
		return servicios.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateScopes = true
		});
	}
}
