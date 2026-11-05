using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Components.Layout;
using FoodTrace.Components.Pages.Compras;
using FoodTrace.Components.Pages.Establecimientos;
using FoodTrace.Components.Pages.Produccion;
using FoodTrace.Components.Pages.Proveedores;
using FoodTrace.Components.Pages.Recetas;
using FoodTrace.Components.Pages.Usuarios;
using FoodTrace.Components.Recetas;
using FoodTrace.Components.Shared;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Establecimientos;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Proveedores;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Models.Administracion;
using FoodTrace.Models.Compras;
using FoodTrace.Models.Recetas;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class WebContractTests
{
    private static ParameterView EnContenedor => ParameterView.FromDictionary(new Dictionary<string, object?> { ["EnContenedor"] = true });
	private sealed class PaginaConflicto : PaginaAdministrativa
	{
		public string Mensaje => Error;

		public PaginaConflicto(ClienteFoodTraceApi cliente, EstadoAdministracion estado)
		{
			Sesion = cliente;
			Catalogos = cliente;
			Formularios = estado;
			Registro = NullLogger<PaginaAdministrativa>.Instance;
		}

		public Task InicializarAsync()
		{
			return OnInitializedAsync();
		}

		public Task ConflictoAsync()
		{
			return EjecutarAsync((CancellationToken _) =>
			{
				throw new ExcepcionApi(HttpStatusCode.Conflict, "El formulario se conserva.");
			});
		}
	}

	private sealed class RecordingHandler : HttpMessageHandler
	{
		public List<string?> Headers { get; } = new List<string?>();

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Headers.Add(request.Headers.Authorization?.ToString());
			// HttpClient resuelve la URI antes de invocar SendAsync.
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
					usuarioActualDto.Id = "test";
					usuarioActualDto.EmpresaId = Guid.NewGuid();
					usuarioActualDto.Correo = "user@example.invalid";
					usuarioActualDto.Roles = new string[1] { "buyer" };
					obj = usuarioActualDto;
				}
				else
				{
					obj = new CatalogosDto
					{
						Empresa = new NombreDto
						{
							Id = Guid.NewGuid(),
							Nombre = "Company"
						}
					};
				}
			}
			else
			{
				obj = new
				{
					accessToken = "isolated-token"
				};
			}
			if (1 == 0)
			{
			}
			object response = obj;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = JsonContent.Create(response)
			});
		}
	}

	[Fact]
	public async Task Recipe_form_requires_an_ingredient_before_enabling_save()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		ClienteFoodTraceApi session = new ClienteFoodTraceApi(app.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		}));
		try
		{
			await session.IniciarSesionAsync(new IniciarSesionDto
			{
				Correo = "admin@test.invalid",
				Contrasena = "Local-test-only!42"
			});
			ProductoDto product = await session.CrearProductoAsync(new CrearProductoDto
			{
				Nombre = "Resultado",
				UnidadBaseId = "kg"
			});
			EstadoAdministracion estado = new EstadoAdministracion(session);
			ModeloVersionReceta modelo = estado.Recetas.Formulario;
			modelo.Nombre = "Receta";
			modelo.Instrucciones = "Preparar";
			modelo.ProductoResultadoId = product.Id;
			await using ServiceProvider services = ServiciosRender.Crear(session, estado);
			await using AsyncServiceScope scope = services.CreateAsyncScope();
			HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
			try
			{
				Dictionary<string, object?> dictionary = new Dictionary<string, object?> { ["Modelo"] = modelo };
				Dictionary<string, object?> dictionary2 = dictionary;
				dictionary2["Catalogos"] = await session.ObtenerCatalogosAsync();
				dictionary["PuedeEditar"] = true;
				ParameterView parameters = ParameterView.FromDictionary(dictionary);
				await renderer.Dispatcher.InvokeAsync(async () =>
				{
					Assert.Matches("(?s)<button[^>]*disabled[^>]*>(?:(?!</button>).)*Guardar borrador", (await renderer.RenderComponentAsync<FormularioVersionReceta>(parameters)).ToHtmlString());
					modelo.Ingredientes.Add(new ModeloIngredienteReceta
					{
						ProductoId = product.Id,
						UnidadMedidaId = "g",
						Cantidad = 150m
					});
					Assert.DoesNotMatch("(?s)<button[^>]*disabled[^>]*>(?:(?!</button>).)*Guardar borrador", (await renderer.RenderComponentAsync<FormularioVersionReceta>(parameters)).ToHtmlString());
					ModeloVersionReceta modeloVersionReceta = modelo;
					Assert.Empty(modeloVersionReceta.Validar(await session.ObtenerCatalogosAsync()));
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
		finally
		{
			if (session != null)
			{
				((IDisposable)session).Dispose();
			}
		}
	}

	[Fact]
	public async Task Module_remount_preserves_drafts_and_shared_alerts_render_in_order()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using ClienteFoodTraceApi session = new ClienteFoodTraceApi(app.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		}));
		await session.IniciarSesionAsync(new IniciarSesionDto
		{
			Correo = "admin@test.invalid",
			Contrasena = "Local-test-only!42"
		});
		await session.CrearProductoAsync(new CrearProductoDto
		{
			Nombre = "Producto",
			UnidadBaseId = "kg"
		});
		EstadoAdministracion estado = new EstadoAdministracion(session);
		estado.Recetas.Formulario.Nombre = "Unsubmitted recipe draft";
		estado.Recetas.Formulario.Instrucciones = "Draft instructions";
		estado.Recetas.Formulario.Ingredientes.Add(new ModeloIngredienteReceta
		{
			Cantidad = 150m
		});
		estado.Establecimiento.Nombre = "Unsubmitted kitchen";
		estado.Compras.Rechazo.Motivo = "Unsubmitted reason";
		Guid necesidadId = Guid.NewGuid();
		estado.Compras.Detalle.Asignaciones[necesidadId] = new ModeloAsignacionCompra
		{
			NecesidadId = necesidadId,
			Cantidad = 25m
		};
		await using ServiceProvider services = ServiciosRender.Crear(session, estado);
		await using AsyncServiceScope scope = services.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				Assert.Contains("Unsubmitted recipe draft", (await renderer.RenderComponentAsync<Recetas>(EnContenedor)).ToHtmlString());
				await renderer.RenderComponentAsync<Produccion>();
				Assert.Contains("Unsubmitted kitchen", (await renderer.RenderComponentAsync<Establecimientos>(EnContenedor)).ToHtmlString());
				Assert.Contains("Crear borrador de compra", WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Compras>()).ToHtmlString()));
				Assert.Contains("Unsubmitted recipe draft", (await renderer.RenderComponentAsync<Recetas>(EnContenedor)).ToHtmlString());
				Assert.Equal(150m, Assert.Single(estado.Recetas.Formulario.Ingredientes).Cantidad);
				Assert.Equal("Unsubmitted reason", estado.Compras.Rechazo.Motivo);
				string html = (await renderer.RenderComponentAsync<AvisosFlujo>(ParameterView.FromDictionary(new Dictionary<string, object?>
				{
					["Error"] = "First error",
					["Aviso"] = "Second notice"
				}))).ToHtmlString();
				Assert.Contains("role=\"alert\"", html);
				Assert.Contains("role=\"status\"", html);
				Assert.True(html.IndexOf("First error", StringComparison.Ordinal) < html.IndexOf("Second notice", StringComparison.Ordinal));
			});
			estado.UsuarioNuevo.Contrasena = "not-a-real-password";
			FormularioUsuario usuarioAnterior = estado.UsuarioNuevo;
			session.CerrarSesion();
			Assert.Empty(usuarioAnterior.Contrasena);
			Assert.Empty(estado.Recetas.Formulario.Ingredientes);
			Assert.Empty(estado.Compras.Detalle.Asignaciones);
			Assert.Null(estado.Recetas.Catalogos);
			Assert.Null(estado.Compras.Catalogos);
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
	public async Task Buyer_render_hides_planner_admin_forms_and_conflict_keeps_selected_drafts()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using ClienteFoodTraceApi session = new ClienteFoodTraceApi(app.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		}));
		await session.IniciarSesionAsync(new IniciarSesionDto
		{
			Correo = "buyer@test.invalid",
			Contrasena = "Local-test-only!42"
		});
		EstadoAdministracion estado = new EstadoAdministracion(session);
		VersionRecetaDto guardada = new VersionRecetaDto
		{
			Id = Guid.NewGuid(),
			Revision = Guid.NewGuid(),
			Nombre = "Saved",
			ProductoResultadoId = Guid.NewGuid(),
			NumeroVersion = 1,
			Rendimiento = 1m,
			Instrucciones = "Instructions",
			Publicada = false,
			Ingredientes = new List<IngredienteRecetaDto>()
		};
		estado.Recetas.Seleccionar(guardada);
		estado.Recetas.Formulario.Nombre = "Unsaved name";
		estado.Compras.Seleccionar(new PedidoCompraDto
		{
			Id = Guid.NewGuid(),
			Revision = Guid.NewGuid(),
			ProveedorId = Guid.NewGuid(),
			Estado = 0,
			CreadoPorId = "creator",
			FechaCreacion = DateTimeOffset.UtcNow,
			Detalles = new List<DetallePedidoCompraDto>()
		});
		estado.Compras.EdicionCantidad = new ModeloCantidadPresentaciones
		{
			DetalleId = Guid.NewGuid(),
			Cantidad = 2m
		};
		Guid necesidadId = Guid.NewGuid();
		estado.Compras.Detalle.Asignaciones[necesidadId] = new ModeloAsignacionCompra
		{
			NecesidadId = necesidadId,
			Cantidad = 1m
		};
		using PaginaConflicto coordinacion = new PaginaConflicto(session, estado);
		await coordinacion.InicializarAsync();
		await coordinacion.ConflictoAsync();
		Assert.Contains("conserva", coordinacion.Mensaje);
		Assert.Same(guardada, estado.Recetas.Seleccionada);
		Assert.NotNull(estado.Compras.Seleccionado);
		Assert.NotNull(estado.Compras.EdicionCantidad);
		Assert.Single(estado.Compras.Detalle.Asignaciones);
		Assert.Equal("Unsaved name", estado.Recetas.Formulario.Nombre);
		await using ServiceProvider services = ServiciosRender.Crear(session, estado);
		await using AsyncServiceScope scope = services.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				Assert.DoesNotContain("Guardar borrador", (await renderer.RenderComponentAsync<Recetas>(EnContenedor)).ToHtmlString());
				Assert.DoesNotContain("Crear usuario", (await renderer.RenderComponentAsync<Usuarios>()).ToHtmlString());
				Assert.DoesNotContain("Crear presentación", WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Proveedores>(EnContenedor)).ToHtmlString()));
				Assert.DoesNotContain("Guardar planificación", WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Produccion>()).ToHtmlString()));
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
	public async Task Web_dtos_and_http_session_complete_login_and_read_api_contracts()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		ClienteFoodTraceApi first = new ClienteFoodTraceApi(app.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		}));
		try
		{
			ClienteFoodTraceApi second = new ClienteFoodTraceApi(app.CreateClient(new WebApplicationFactoryClientOptions
			{
				BaseAddress = new Uri("https://localhost")
			}));
			try
			{
				await first.IniciarSesionAsync(new IniciarSesionDto
				{
					Correo = "admin@test.invalid",
					Contrasena = "Local-test-only!42"
				});
				Assert.True(first.TieneRol("admin"));
				Assert.Null(second.Identidad);
				await Assert.ThrowsAsync<ExcepcionApi>(() => second.ObtenerCatalogosAsync());
				ProductoDto product = await first.CrearProductoAsync(new CrearProductoDto
				{
					Nombre = "Output",
					UnidadBaseId = "unit"
				});
				Assert.Contains((IEnumerable<ProductoDto>)(await first.ObtenerCatalogosAsync()).Productos, (Predicate<ProductoDto>)((ProductoDto x) => x.Id == product.Id && x.UnidadBaseId == "unit"));
				await second.IniciarSesionAsync(new IniciarSesionDto
				{
					Correo = "other@test.invalid",
					Contrasena = "Local-test-only!42"
				});
				Assert.Empty((await second.ObtenerCatalogosAsync()).Productos);
				// IniciarSesionAsync completó el contrato de identidad de ambas sesiones.
				Assert.NotEqual(first.Identidad!.EmpresaId, second.Identidad!.EmpresaId);
				Assert.Empty(await first.ObtenerRecetasAsync());
				Assert.Empty(await first.ObtenerProduccionAsync());
				NombreDto supplier = await first.CrearProveedorAsync(new CrearProveedorDto
				{
					Nombre = "Supplier"
				});
				PedidoCompraDto purchase = await first.CrearPedidoCompraAsync(new CrearPedidoCompraDto
				{
					ProveedorId = supplier.Id
				});
				Assert.Equal(0, purchase.Estado);
				Assert.Equal(first.Identidad.Id, purchase.CreadoPorId);
				Assert.Contains((IEnumerable<PedidoCompraDto>)(await first.ObtenerPedidosCompraAsync()), (Predicate<PedidoCompraDto>)((PedidoCompraDto x) => x.Id == purchase.Id));
				NombreDto site = await first.CrearEstablecimientoAsync(new CrearEstablecimientoDto
				{
					Nombre = "Kitchen"
				});
				PresentacionProveedorDto pack = await first.CrearPresentacionAsync(new CrearPresentacionProveedorDto
				{
					ProveedorId = supplier.Id,
					ProductoId = product.Id,
					Nombre = "Unit pack",
					UnidadMedidaId = "unit",
					Cantidad = 1m
				});
				VersionRecetaDto recipe = await first.CrearRecetaAsync(new CrearRecetaDto
				{
					Nombre = "Recipe",
					ProductoResultadoId = product.Id,
					Rendimiento = 1m,
					Instrucciones = "Assemble",
					Ingredientes = new IngredienteRecetaEntradaDto[1]
					{
						new IngredienteRecetaEntradaDto
						{
							ProductoId = product.Id,
							UnidadMedidaId = "unit",
							Cantidad = 1m
						}
					}
				});
				Assert.Single(recipe.Ingredientes);
				recipe = await first.PublicarRecetaAsync(recipe.Id, new RevisionDto
				{
					Revision = recipe.Revision
				});
				Assert.True(recipe.Publicada);
				NecesidadIngredienteDto need = Assert.Single((await first.PlanificarProduccionAsync(new PlanificarProduccionDto
				{
					EstablecimientoId = site.Id,
					VersionRecetaId = recipe.Id,
					CantidadPlanificada = 2m,
					UnidadMedidaId = "unit",
					FechaPlanificada = new DateOnly(2026, 10, 12)
				})).Necesidades);
				Assert.Equal(2m, need.CantidadNecesaria);
				purchase = await first.AgregarDetalleCompraAsync(purchase.Id, new AgregarDetalleCompraDto
				{
					Revision = purchase.Revision,
					PresentacionProveedorId = pack.Id,
					CantidadPresentaciones = 2m,
					Asignaciones = new AsignacionNecesidadEntradaDto[1]
					{
						new AsignacionNecesidadEntradaDto
						{
							NecesidadIngredienteId = need.Id,
							CantidadAsignada = need.CantidadNecesaria,
							Revision = need.Revision
						}
					}
				});
				DetallePedidoCompraDto line = Assert.Single(purchase.Detalles);
				Assert.Equal(2m, line.CantidadPedida);
				Assert.Equal(need.Id, Assert.Single(line.Asignaciones).NecesidadIngredienteId);
				purchase = await first.EnviarPedidoCompraAsync(purchase.Id, new RevisionDto
				{
					Revision = purchase.Revision
				});
				Assert.Equal(1, purchase.Estado);
				purchase = await first.AprobarPedidoCompraAsync(purchase.Id, new RevisionDto
				{
					Revision = purchase.Revision
				});
				Assert.Equal(2, purchase.Estado);
				Assert.NotNull(purchase.FechaDecision);
				Assert.Equal(first.Identidad.Id, purchase.DecididoPorId);
				first.CerrarSesion();
				await Assert.ThrowsAsync<ExcepcionApi>(() => first.ObtenerCatalogosAsync());
				Assert.NotNull(second.Identidad);
			}
			finally
			{
				if (second != null)
				{
					((IDisposable)second).Dispose();
				}
			}
		}
		finally
		{
			if (first != null)
			{
				((IDisposable)first).Dispose();
			}
		}
	}

	[Fact]
	public async Task Bearer_headers_are_per_request_not_shared_default_headers()
	{
		RecordingHandler handler = new RecordingHandler();
		using HttpClient client = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://example.invalid")
		};
		using ClienteFoodTraceApi session = new ClienteFoodTraceApi(client);
		await session.IniciarSesionAsync(new IniciarSesionDto
		{
			Correo = "user@example.invalid",
			Contrasena = "not-a-real-password"
		});
		await session.ObtenerCatalogosAsync();
		Assert.Null(client.DefaultRequestHeaders.Authorization);
		Assert.Equal(new string?[3] { null, "Bearer isolated-token", "Bearer isolated-token" }, handler.Headers);
		session.CerrarSesion();
		await session.ObtenerCatalogosAsync();
		Assert.Null(handler.Headers.Last());
	}
}
