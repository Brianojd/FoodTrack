using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Components.Pages.Produccion;
using FoodTrace.Components.Pages.Recetas;
using FoodTrace.Components.Produccion;
using FoodTrace.Components.Recetas;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Dtos.UnidadesMedida;
using FoodTrace.Models.Produccion;
using FoodTrace.Models.Recetas;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class RenderRecetasProduccionTests
{
	private sealed class Transporte(string rol) : HttpMessageHandler
	{
		private readonly Guid productoId = Guid.NewGuid();

		private readonly Guid versionId = Guid.NewGuid();

		private readonly Guid sitioId = Guid.NewGuid();

		public Guid OrdenId { get; } = Guid.NewGuid();

		public bool FallarNecesidades { get; set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// El transporte solo recibe solicitudes ya resueltas por HttpClient.
			if (FallarNecesidades && request.RequestUri!.AbsolutePath == "/api/needs")
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
			}
			NecesidadIngredienteDto necesidad = new NecesidadIngredienteDto
			{
				Id = Guid.NewGuid(),
				OrdenProduccionId = OrdenId,
				ProductoId = productoId,
				UnidadBaseId = "kg",
				CantidadNecesaria = 150m,
				CantidadAsignada = 7m
			};
			string absolutePath = request.RequestUri!.AbsolutePath;
			if (1 == 0)
			{
			}
			object obj;
			switch (absolutePath)
			{
			case "/auth/login":
				obj = new
				{
					accessToken = "token-de-prueba"
				};
				break;
			case "/api/me":
			{
				UsuarioActualDto usuarioActualDto = new UsuarioActualDto();
				usuarioActualDto.Id = "cuenta";
				usuarioActualDto.Roles = new string[1] { rol };
				obj = usuarioActualDto;
				break;
			}
			case "/api/recipes":
			{
				List<VersionRecetaDto> list6 = new List<VersionRecetaDto>();
				VersionRecetaDto versionRecetaDto = new VersionRecetaDto
				{
					Id = versionId,
					FamiliaRecetaId = Guid.NewGuid(),
					Nombre = "Receta guardada",
					Publicada = true,
					NumeroVersion = 1,
					ProductoResultadoId = productoId,
					Rendimiento = 20m
				};
				int num = 1;
				List<IngredienteRecetaDto> list7 = new List<IngredienteRecetaDto>(num);
				CollectionsMarshal.SetCount(list7, num);
				CollectionsMarshal.AsSpan(list7)[0] = new IngredienteRecetaDto
				{
					ProductoId = productoId,
					UnidadMedidaId = "kg",
					Cantidad = 987m
				};
				versionRecetaDto.Ingredientes = list7;
				list6.Add(versionRecetaDto);
				obj = list6;
				break;
			}
			case "/api/production":
			{
				List<OrdenProduccionDto> list4 = new List<OrdenProduccionDto>();
				OrdenProduccionDto ordenProduccionDto = new OrdenProduccionDto
				{
					Id = OrdenId,
					VersionRecetaId = versionId,
					EstablecimientoId = sitioId,
					CantidadPlanificada = 2m,
					FechaPlanificada = new DateOnly(2026, 10, 15)
				};
				int num = 1;
				List<NecesidadIngredienteDto> list5 = new List<NecesidadIngredienteDto>(num);
				CollectionsMarshal.SetCount(list5, num);
				CollectionsMarshal.AsSpan(list5)[0] = necesidad;
				ordenProduccionDto.Necesidades = list5;
				list4.Add(ordenProduccionDto);
				obj = list4;
				break;
			}
			case "/api/needs":
				obj = new List<NecesidadIngredienteDto> { necesidad };
				break;
			default:
			{
				CatalogosDto catalogosDto = new CatalogosDto();
				int num = 1;
				List<ProductoDto> list = new List<ProductoDto>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = new ProductoDto
				{
					Id = productoId,
					Nombre = "Carne",
					UnidadBaseId = "kg"
				};
				catalogosDto.Productos = list;
				num = 1;
				List<NombreDto> list2 = new List<NombreDto>(num);
				CollectionsMarshal.SetCount(list2, num);
				CollectionsMarshal.AsSpan(list2)[0] = new NombreDto
				{
					Id = sitioId,
					Nombre = "Cocina"
				};
				catalogosDto.Establecimientos = list2;
				num = 1;
				List<UnidadMedidaDto> list3 = new List<UnidadMedidaDto>(num);
				CollectionsMarshal.SetCount(list3, num);
				CollectionsMarshal.AsSpan(list3)[0] = new UnidadMedidaDto
				{
					Id = "kg",
					Dimension = "mass",
					FactorConversion = 1m
				};
				catalogosDto.UnidadesMedida = list3;
				obj = catalogosDto;
				break;
			}
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
	[InlineData(new object[] { 1 })]
	[InlineData(new object[] { 12 })]
	public async Task La_version_publicada_se_identifica_con_su_numero_real_en_planificacion(int numero)
	{
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new Transporte("planner"))
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			Guid versionId = Guid.NewGuid();
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<FormularioPlanificacion>(ParameterView.FromDictionary(new Dictionary<string, object?>
				{
					["Modelo"] = new ModeloPlanificacion
					{
						VersionRecetaId = versionId
					},
					["Catalogos"] = new CatalogosDto
					{
						Establecimientos = new List<NombreDto>(1)
						{
							new NombreDto
							{
								Id = Guid.NewGuid(),
								Nombre = "Cocina"
							}
						}
					},
					["Versiones"] = new List<VersionRecetaDto>
					{
						new VersionRecetaDto
						{
							Id = versionId,
							Nombre = "Hamburguesa",
							Publicada = true,
							NumeroVersion = numero
						}
					}
				}))).ToHtmlString());
				Assert.Contains($"Hamburguesa · v{numero}", html);
				Assert.DoesNotContain("@version.NumeroVersion", html);
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
	public async Task Una_publicada_no_renderiza_campos_editables_aunque_el_usuario_sea_planificador()
	{
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new Transporte("planner"))
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			VersionRecetaDto version = new VersionRecetaDto
			{
				Publicada = true,
				Nombre = "Versión oficial",
				NumeroVersion = 3,
				FamiliaRecetaId = Guid.NewGuid()
			};
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<FormularioVersionReceta>(ParameterView.FromDictionary(new Dictionary<string, object?>
				{
					["Modelo"] = new ModeloVersionReceta
					{
						Nombre = "No debe publicarse ni editarse"
					},
					["Version"] = version,
					["Catalogos"] = new CatalogosDto(),
					["PuedeEditar"] = true
				}))).ToHtmlString());
				Assert.Contains("Versión oficial", html);
				Assert.Contains("solo lectura", html);
				Assert.DoesNotContain("<input", html);
				Assert.DoesNotContain("<textarea", html);
				Assert.DoesNotContain("Guardar borrador", html);
				Assert.DoesNotContain("No debe publicarse ni editarse", html);
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
	[InlineData(new object[] { "planner", true })]
	[InlineData(new object[] { "admin", false })]
	[InlineData(new object[] { "buyer", false })]
	public async Task Solo_planner_crea_y_las_necesidades_se_muestran_sin_recalcular(string rol, bool puedePlanificar)
	{
		Transporte transporte = new Transporte(rol);
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(transporte)
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				// Editors are modal now; render the embedded area and assert its entry action,
				// while keeping the original role and server-persisted quantity checks.
				HtmlRootComponent recetas = await renderer.RenderComponentAsync<Recetas>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["EnContenedor"] = true }));
				Assert.Equal(puedePlanificar, WebUtility.HtmlDecode(recetas.ToHtmlString()).Contains(">Crear receta</span>", StringComparison.Ordinal));
				string html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Produccion>()).ToHtmlString());
				Assert.Equal(puedePlanificar, html.Contains(">Planificar producción</span>", StringComparison.Ordinal));
				Assert.Contains("150 kg", html);
				Assert.Contains("7 kg", html);
				Assert.Contains("Necesidad bruta guardada", html);
				Assert.Contains($"/produccion?orden={transporte.OrdenId}", html);
				Assert.DoesNotContain("987 kg", html);
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
	public async Task Una_carga_fallida_conserva_atomicamente_todos_los_datos_anteriores()
	{
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient(new Transporte("buyer")
		{
			FallarNecesidades = true
		})
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		CatalogosDto catalogos = new CatalogosDto
		{
			Empresa = new NombreDto
			{
				Nombre = "Última empresa válida"
			}
		};
		List<VersionRecetaDto> versiones = new List<VersionRecetaDto>();
		List<OrdenProduccionDto> ordenes = new List<OrdenProduccionDto>();
		List<NecesidadIngredienteDto> necesidades = new List<NecesidadIngredienteDto>();
		estado.Produccion.Catalogos = catalogos;
		estado.Produccion.Versiones = versiones;
		estado.Produccion.Ordenes = ordenes;
		estado.Produccion.Necesidades = necesidades;
		estado.Produccion.Formulario.Cantidad = 456m;
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				Assert.Contains("role=\"alert\"", (await renderer.RenderComponentAsync<Produccion>()).ToHtmlString());
			});
			Assert.Same(catalogos, estado.Produccion.Catalogos);
			Assert.Same(versiones, estado.Produccion.Versiones);
			Assert.Same(ordenes, estado.Produccion.Ordenes);
			Assert.Same(necesidades, estado.Produccion.Necesidades);
			Assert.Equal(456m, estado.Produccion.Formulario.Cantidad);
		}
		finally
		{
			if (renderer != null)
			{
				await renderer.DisposeAsync();
			}
		}
	}
}
