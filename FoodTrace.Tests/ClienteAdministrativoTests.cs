using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Models.Administracion;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class ClienteAdministrativoTests
{
	private sealed class CatalogoPendiente : HttpMessageHandler
	{
		// HttpClient establece una URI absoluta antes de invocar el transporte.
		public TaskCompletionSource<HttpResponseMessage> Pendiente { get; } = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (request.RequestUri!.AbsolutePath == "/api/catalogs")
			{
				return Pendiente.Task;
			}
			object cuerpo = ((request.RequestUri.AbsolutePath == "/auth/login") ? ((object)new
			{
				accessToken = "token-de-prueba"
			}) : ((object)new
			{
				id = "cuenta",
				companyId = Guid.NewGuid(),
				email = "cuenta@example.invalid",
				roles = new string[1] { "buyer" }
			}));
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = JsonContent.Create(cuerpo)
			});
		}
	}

	private sealed class LoginPendiente(TaskCompletionSource<HttpResponseMessage> pendiente) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return pendiente.Task;
		}
	}

	private sealed class Respuestas(HttpStatusCode estado) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// Contrato de HttpClient: la URI ya está resuelta antes de SendAsync.
			string absolutePath = request.RequestUri!.AbsolutePath;
			if (1 == 0)
			{
			}
			object obj;
			if (absolutePath == "/auth/login")
			{
				obj = new
				{
					accessToken = "token-de-prueba"
				};
			}
			else
			{
				obj = ((!(absolutePath == "/api/me")) ? ((object)new
				{
					message = "INTERNAL stack trace"
				}) : ((object)new
				{
					id = "cuenta",
					companyId = Guid.NewGuid(),
					email = "cuenta@example.invalid",
					roles = new string[1] { "buyer" }
				}));
			}
			if (1 == 0)
			{
			}
			object cuerpo = obj;
			HttpStatusCode codigo = (request.RequestUri.AbsolutePath.StartsWith("/api/catalogs") ? estado : HttpStatusCode.OK);
			return Task.FromResult(new HttpResponseMessage(codigo)
			{
				Content = JsonContent.Create(cuerpo)
			});
		}
	}

	[Fact]
	public void Las_interfaces_comparten_un_cliente_por_scope_sin_compartir_sesiones()
	{
		IServiceCollection registros = new ServiceCollection().AddLogging();
		registros.AddHttpClient("FoodTrace", (HttpClient httpClient) =>
		{
			httpClient.BaseAddress = new Uri("https://example.invalid");
		});
		registros.AgregarClienteFoodTrace();
		using ServiceProvider servicios = registros.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateScopes = true
		});
		using IServiceScope primero = servicios.CreateScope();
		using IServiceScope segundo = servicios.CreateScope();
		ClienteFoodTraceApi cliente = primero.ServiceProvider.GetRequiredService<ClienteFoodTraceApi>();
		Assert.Same(cliente, primero.ServiceProvider.GetRequiredService<ISesionFoodTrace>());
		Assert.Same(cliente, primero.ServiceProvider.GetRequiredService<ICatalogosFoodTrace>());
		Assert.Same(cliente, primero.ServiceProvider.GetRequiredService<IUsuariosFoodTrace>());
		Assert.Same(cliente, primero.ServiceProvider.GetRequiredService<IServicioRecetas>());
		Assert.Same(cliente, primero.ServiceProvider.GetRequiredService<IServicioProduccion>());
		Assert.Same(cliente, primero.ServiceProvider.GetRequiredService<IServicioCompras>());
		Assert.NotSame(cliente, segundo.ServiceProvider.GetRequiredService<ISesionFoodTrace>());
	}

	[Fact]
	public async Task Los_borradores_pertenecen_a_la_sesion_y_las_contrasenas_se_limpian_al_cerrarla()
	{
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new Respuestas(HttpStatusCode.OK))
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		estado.Establecimiento.Nombre = "Borrador sin enviar";
		estado.UsuarioNuevo.Contrasena = "solo-prueba";
		estado.Recetas.Formulario.Nombre = "Receta sin guardar";
		estado.Produccion.Formulario.Cantidad = 1000m;
		FormularioUsuario usuarioAnterior = estado.UsuarioNuevo;
		Assert.Equal("Borrador sin enviar", estado.Establecimiento.Nombre);
		cliente.CerrarSesion();
		Assert.Empty(estado.Establecimiento.Nombre);
		Assert.Empty(usuarioAnterior.Contrasena);
		Assert.NotSame(usuarioAnterior, estado.UsuarioNuevo);
		Assert.Empty(estado.UsuarioNuevo.Contrasena);
		Assert.Empty(estado.Recetas.Formulario.Nombre);
		Assert.Equal(1m, estado.Produccion.Formulario.Cantidad);
	}

	[Theory]
	[InlineData(new object[]
	{
		HttpStatusCode.Forbidden,
		true
	})]
	[InlineData(new object[]
	{
		HttpStatusCode.Conflict,
		true
	})]
	[InlineData(new object[]
	{
		HttpStatusCode.Unauthorized,
		false
	})]
	public async Task Los_errores_conservan_sesion_salvo_401_y_no_exponen_el_backend(HttpStatusCode estado, bool conserva)
	{
		ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new Respuestas(estado))
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		try
		{
			await cliente.IniciarSesionAsync(new IniciarSesionDto
			{
				Correo = "cuenta@example.invalid",
				Contrasena = "solo-prueba-no-real"
			});
			ExcepcionApi error = await Assert.ThrowsAsync<ExcepcionApi>(() => cliente.ObtenerCatalogosAsync());
			Assert.Equal(estado, error.Estado);
			Assert.DoesNotContain("INTERNAL", error.Message);
			Assert.Equal(conserva, cliente.Identidad != null);
			if (estado == HttpStatusCode.Conflict)
			{
				Assert.Contains("conserv", error.Message);
			}
		}
		finally
		{
			if (cliente != null)
			{
				((IDisposable)cliente).Dispose();
			}
		}
	}

	[Fact]
	public async Task Una_respuesta_anterior_al_logout_no_restablece_la_identidad()
	{
		TaskCompletionSource<HttpResponseMessage> pendiente = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new LoginPendiente(pendiente))
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		Task<UsuarioActualDto> inicio = cliente.IniciarSesionAsync(new IniciarSesionDto());
		cliente.CerrarSesion();
		pendiente.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = JsonContent.Create(new
			{
				accessToken = "token-de-prueba"
			})
		});
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => inicio);
		Assert.Null(cliente.Identidad);
	}

	[Fact]
	public async Task Un_401_tardio_no_cierra_una_sesion_posterior()
	{
		CatalogoPendiente transporte = new CatalogoPendiente();
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(transporte)
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		Task<CatalogosDto> anterior = cliente.ObtenerCatalogosAsync();
		cliente.CerrarSesion();
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		UsuarioActualDto? identidadActual = cliente.Identidad;
		transporte.Pendiente.SetResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => anterior);
		Assert.Same(identidadActual, cliente.Identidad);
		Assert.NotNull(cliente.Identidad);
	}
}
