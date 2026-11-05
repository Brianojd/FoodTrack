using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Components.Layout;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Models.Administracion;
using FoodTrace.Services.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class PaginaAdministrativaTests
{
	private sealed class PaginaPrueba : PaginaAdministrativa
	{
		public FormularioNombre Formulario { get; } = new FormularioNombre();

		public CatalogosDto? Catalogo => Datos;

		public string Mensaje => Error;

		public bool EstaOcupada => Ocupado;

		public PaginaPrueba(ClienteFoodTraceApi cliente)
		{
			Sesion = cliente;
			Catalogos = cliente;
			Registro = NullLogger<PaginaAdministrativa>.Instance;
		}

		public Task InicializarAsync()
		{
			return OnInitializedAsync();
		}

		public Task ActualizarDatosAsync()
		{
			return ActualizarAsync();
		}

		public Task EjecutarPruebaAsync(Func<CancellationToken, Task> accion)
		{
			return EjecutarAsync(accion);
		}
	}

	private sealed class Transporte : HttpMessageHandler
	{
		public HttpStatusCode Estado { get; set; } = HttpStatusCode.OK;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// HttpClient resuelve la URI antes de llamar al transporte.
			string ruta = request.RequestUri!.AbsolutePath;
			if (1 == 0)
			{
			}
			object obj;
			if (!(ruta == "/auth/login"))
			{
				if (ruta == "/api/me")
				{
					UsuarioActualDto usuarioActualDto = new UsuarioActualDto();
					usuarioActualDto.Id = "cuenta";
					usuarioActualDto.Roles = new string[1] { "admin" };
					obj = usuarioActualDto;
				}
				else
				{
					obj = new CatalogosDto
					{
						Empresa = new NombreDto
						{
							Nombre = "Empresa de prueba"
						}
					};
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
			return Task.FromResult(new HttpResponseMessage((ruta == "/api/catalogs") ? Estado : HttpStatusCode.OK)
			{
				Content = JsonContent.Create(cuerpo)
			});
		}
	}

	[Fact]
	public async Task La_actualizacion_fallida_y_el_conflicto_conservan_datos_y_formulario_sin_repetir_envios()
	{
		Transporte transporte = new Transporte();
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(transporte)
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using PaginaPrueba pagina = new PaginaPrueba(cliente);
		await pagina.InicializarAsync();
		CatalogosDto? datos = pagina.Catalogo;
		pagina.Formulario.Nombre = "Nombre sin guardar";
		transporte.Estado = HttpStatusCode.ServiceUnavailable;
		await pagina.ActualizarDatosAsync();
		Assert.Same(datos, pagina.Catalogo);
		Assert.NotEmpty(pagina.Mensaje);
		int envios = 0;
		await pagina.EjecutarPruebaAsync((CancellationToken _) =>
		{
			envios++;
			throw new ExcepcionApi(HttpStatusCode.Conflict, "El formulario se conserva.");
		});
		Assert.Equal(1, envios);
		Assert.Equal("Nombre sin guardar", pagina.Formulario.Nombre);
		Assert.Same(datos, pagina.Catalogo);
		Assert.Contains("conserv", pagina.Mensaje);
	}

	[Fact]
	public async Task Un_segundo_envio_no_se_ejecuta_mientras_hay_una_operacion_pendiente()
	{
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new Transporte())
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using PaginaPrueba pagina = new PaginaPrueba(cliente);
		await pagina.InicializarAsync();
		TaskCompletionSource pendiente = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		int envios = 0;
		Task primero = pagina.EjecutarPruebaAsync(async (CancellationToken _) =>
		{
			envios++;
			await pendiente.Task;
		});
		await pagina.EjecutarPruebaAsync((CancellationToken _) =>
		{
			envios++;
			return Task.CompletedTask;
		});
		Assert.True(pagina.EstaOcupada);
		Assert.Equal(1, envios);
		pendiente.SetResult();
		await primero;
		Assert.False(pagina.EstaOcupada);
	}

	[Fact]
	public async Task Disponer_la_pagina_cancela_su_operacion_pendiente()
	{
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new Transporte())
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		PaginaPrueba pagina = new PaginaPrueba(cliente);
		await pagina.InicializarAsync();
		bool cancelada = false;
		Task operacion = pagina.EjecutarPruebaAsync(async (CancellationToken ct) =>
		{
			try
			{
				await Task.Delay(-1, ct);
			}
			catch (OperationCanceledException)
			{
				cancelada = true;
				throw;
			}
		});
		pagina.Dispose();
		await operacion;
		Assert.True(cancelada);
		Assert.Empty(pagina.Mensaje);
	}
}
