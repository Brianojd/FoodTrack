using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Establecimientos;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Dtos.UnidadesMedida;
using FoodTrace.Models.Produccion;
using FoodTrace.Models.Recetas;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class RecetasProduccionTests
{
	[Fact]
	public void La_validacion_exige_ingredientes_y_preserva_lineas_duplicadas_con_claves_estables()
	{
		Guid producto = Guid.NewGuid();
		ModeloVersionReceta modelo = new ModeloVersionReceta
		{
			Nombre = "Receta",
			ProductoResultadoId = producto,
			Rendimiento = 1m,
			Instrucciones = "Preparar"
		};
		Assert.False(Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true));
		modelo.Ingredientes.Add(new ModeloIngredienteReceta
		{
			ProductoId = producto,
			UnidadMedidaId = "g",
			Cantidad = 150m
		});
		modelo.Ingredientes.Add(new ModeloIngredienteReceta
		{
			ProductoId = producto,
			UnidadMedidaId = "g",
			Cantidad = 20m
		});
		Assert.True(Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true));
		Assert.NotEqual(modelo.Ingredientes[0].Clave, modelo.Ingredientes[1].Clave);
		Guid clave = modelo.Ingredientes[0].Clave;
		modelo.Ingredientes[0].Cantidad = 0.0000001m;
		Assert.False(Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true));
		Assert.Equal(clave, modelo.Ingredientes[0].Clave);
	}

	[Fact]
	public void Editar_un_modelo_no_modifica_la_version_guardada_y_actualizar_la_lista_no_descarta_cambios()
	{
		VersionRecetaDto versionRecetaDto = new VersionRecetaDto
		{
			Id = Guid.NewGuid(),
			FamiliaRecetaId = Guid.NewGuid(),
			Revision = Guid.NewGuid(),
			Nombre = "Guardada",
			Rendimiento = 1m
		};
		int num = 1;
		List<IngredienteRecetaDto> list = new List<IngredienteRecetaDto>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = new IngredienteRecetaDto
		{
			Cantidad = 150m
		};
		versionRecetaDto.Ingredientes = list;
		VersionRecetaDto guardada = versionRecetaDto;
		EstadoRecetas estado = new EstadoRecetas();
		estado.Seleccionar(guardada);
		estado.Formulario.Nombre = "Sin guardar";
		estado.Formulario.Ingredientes[0].Cantidad = 200m;
		num = 1;
		List<VersionRecetaDto> list2 = new List<VersionRecetaDto>(num);
		CollectionsMarshal.SetCount(list2, num);
		CollectionsMarshal.AsSpan(list2)[0] = new VersionRecetaDto
		{
			Id = guardada.Id,
			Revision = Guid.NewGuid(),
			Nombre = "Actualizada por otro usuario"
		};
		estado.Versiones = list2;
		Assert.True(estado.TieneCambios);
		Assert.Equal("Guardada", guardada.Nombre);
		Assert.Equal(150m, guardada.Ingredientes[0].Cantidad);
		// Seleccionar conserva la versión guardada mientras el listado se actualiza.
		Assert.Equal(guardada.Revision, estado.Seleccionada!.Revision);
		Assert.Equal("Sin guardar", estado.Formulario.Nombre);
	}

	[Fact]
	public void Cambiar_seleccion_invalida_la_generacion_y_cancela_la_carga_anterior()
	{
		using CancellationTokenSource vida = new CancellationTokenSource();
		using ControlCargaModulo cargas = new ControlCargaModulo();
		(long, CancellationToken) anterior = cargas.Iniciar(vida.Token);
		cargas.Cancelar();
		Assert.True(anterior.Item2.IsCancellationRequested);
		Assert.False(cargas.EsActual(anterior.Item1));
		(long, CancellationToken) actual = cargas.Iniciar(vida.Token);
		Assert.True(cargas.EsActual(actual.Item1));
		Assert.False(cargas.EsActual(anterior.Item1));
		vida.Cancel();
		Assert.True(actual.Item2.IsCancellationRequested);
		Assert.False(cargas.EsActual(actual.Item1));
	}

	[Fact]
	public void Planificacion_rechaza_versiones_no_publicadas_y_unidades_incompatibles()
	{
		Guid sitio = Guid.NewGuid();
		Guid resultado = Guid.NewGuid();
		Guid versionId = Guid.NewGuid();
		CatalogosDto catalogosDto = new CatalogosDto();
		int num = 1;
		List<NombreDto> list = new List<NombreDto>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = new NombreDto
		{
			Id = sitio
		};
		catalogosDto.Establecimientos = list;
		num = 1;
		List<ProductoDto> list2 = new List<ProductoDto>(num);
		CollectionsMarshal.SetCount(list2, num);
		CollectionsMarshal.AsSpan(list2)[0] = new ProductoDto
		{
			Id = resultado,
			UnidadBaseId = "kg"
		};
		catalogosDto.Productos = list2;
		num = 3;
		List<UnidadMedidaDto> list3 = new List<UnidadMedidaDto>(num);
		CollectionsMarshal.SetCount(list3, num);
		Span<UnidadMedidaDto> span = CollectionsMarshal.AsSpan(list3);
		span[0] = new UnidadMedidaDto
		{
			Id = "kg",
			Dimension = "mass"
		};
		span[1] = new UnidadMedidaDto
		{
			Id = "g",
			Dimension = "mass"
		};
		span[2] = new UnidadMedidaDto
		{
			Id = "L",
			Dimension = "volume"
		};
		catalogosDto.UnidadesMedida = list3;
		CatalogosDto catalogos = catalogosDto;
		VersionRecetaDto version = new VersionRecetaDto
		{
			Id = versionId,
			ProductoResultadoId = resultado
		};
		ModeloPlanificacion formulario = new ModeloPlanificacion
		{
			EstablecimientoId = sitio,
			VersionRecetaId = versionId,
			Fecha = new DateTime(2026, 10, 15),
			Cantidad = 1m,
			UnidadMedidaId = "kg"
		};
		num = 1;
		List<VersionRecetaDto> list4 = new List<VersionRecetaDto>(num);
		CollectionsMarshal.SetCount(list4, num);
		CollectionsMarshal.AsSpan(list4)[0] = version;
		Assert.NotEmpty(formulario.Validar(catalogos, list4));
		version.Publicada = true;
		formulario.UnidadMedidaId = "L";
		num = 1;
		List<VersionRecetaDto> list5 = new List<VersionRecetaDto>(num);
		CollectionsMarshal.SetCount(list5, num);
		CollectionsMarshal.AsSpan(list5)[0] = version;
		Assert.NotEmpty(formulario.Validar(catalogos, list5));
		formulario.UnidadMedidaId = "g";
		num = 1;
		List<VersionRecetaDto> list6 = new List<VersionRecetaDto>(num);
		CollectionsMarshal.SetCount(list6, num);
		CollectionsMarshal.AsSpan(list6)[0] = version;
		Assert.Empty(formulario.Validar(catalogos, list6));
		Assert.Equal(new DateOnly(2026, 10, 15), formulario.ParaEnviar().FechaPlanificada);
		formulario.Fecha = null;
		num = 1;
		List<VersionRecetaDto> list7 = new List<VersionRecetaDto>(num);
		CollectionsMarshal.SetCount(list7, num);
		CollectionsMarshal.AsSpan(list7)[0] = version;
		Assert.NotEmpty(formulario.Validar(catalogos, list7));
	}

	[Fact]
	public async Task La_cantidad_guardada_de_produccion_esta_en_unidad_base_no_en_la_unidad_ingresada()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(app.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		}));
		await cliente.IniciarSesionAsync(new IniciarSesionDto
		{
			Correo = "admin@test.invalid",
			Contrasena = "Local-test-only!42"
		});
		ProductoDto producto = await cliente.CrearProductoAsync(new CrearProductoDto
		{
			Nombre = "Preparado",
			UnidadBaseId = "kg"
		});
		NombreDto cocina = await cliente.CrearEstablecimientoAsync(new CrearEstablecimientoDto
		{
			Nombre = "Cocina"
		});
		VersionRecetaDto receta = await cliente.CrearRecetaAsync(new CrearRecetaDto
		{
			Nombre = "Preparado",
			ProductoResultadoId = producto.Id,
			Rendimiento = 1m,
			Instrucciones = "Preparar.",
			Ingredientes = new IngredienteRecetaEntradaDto[1]
			{
				new IngredienteRecetaEntradaDto
				{
					ProductoId = producto.Id,
					UnidadMedidaId = "g",
					Cantidad = 1000m
				}
			}
		});
		receta = await cliente.PublicarRecetaAsync(receta.Id, new RevisionDto
		{
			Revision = receta.Revision
		});
		OrdenProduccionDto orden = await cliente.PlanificarProduccionAsync(new PlanificarProduccionDto
		{
			EstablecimientoId = cocina.Id,
			VersionRecetaId = receta.Id,
			CantidadPlanificada = 2000m,
			UnidadMedidaId = "g",
			FechaPlanificada = new DateOnly(2026, 10, 15)
		});
		Assert.Equal(2m, orden.CantidadPlanificada);
		NecesidadIngredienteDto necesidad = Assert.Single(orden.Necesidades);
		Assert.Equal("kg", necesidad.UnidadBaseId);
		Assert.Equal(2m, necesidad.CantidadNecesaria);
	}

	[Theory]
	[InlineData(new object[] { 1, 150, 20 })]
	[InlineData(new object[] { 20, 3000, 400 })]
	public async Task El_cliente_tipado_planifica_mil_hamburguesas_con_necesidades_persistidas_y_version_congelada(int rendimiento, int carneGramos, int quesoGramos)
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(app.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		}));
		try
		{
			await cliente.IniciarSesionAsync(new IniciarSesionDto
			{
				Correo = "admin@test.invalid",
				Contrasena = "Local-test-only!42"
			});
			ProductoDto hamburguesa = await cliente.CrearProductoAsync(new CrearProductoDto
			{
				Nombre = "Hamburguesa",
				UnidadBaseId = "unit"
			});
			ProductoDto carne = await cliente.CrearProductoAsync(new CrearProductoDto
			{
				Nombre = "Carne",
				UnidadBaseId = "kg"
			});
			ProductoDto queso = await cliente.CrearProductoAsync(new CrearProductoDto
			{
				Nombre = "Queso",
				UnidadBaseId = "kg"
			});
			NombreDto cocina = await cliente.CrearEstablecimientoAsync(new CrearEstablecimientoDto
			{
				Nombre = "Cocina"
			});
			VersionRecetaDto receta = await cliente.CrearRecetaAsync(new CrearRecetaDto
			{
				Nombre = "Hamburguesas",
				ProductoResultadoId = hamburguesa.Id,
				Rendimiento = rendimiento,
				Instrucciones = "Preparar y cocinar.",
				Ingredientes = new IngredienteRecetaEntradaDto[2]
				{
					new IngredienteRecetaEntradaDto
					{
						ProductoId = carne.Id,
						UnidadMedidaId = "g",
						Cantidad = carneGramos
					},
					new IngredienteRecetaEntradaDto
					{
						ProductoId = queso.Id,
						UnidadMedidaId = "g",
						Cantidad = quesoGramos
					}
				}
			});
			Guid revisionInicial = receta.Revision;
			receta = await cliente.PublicarRecetaAsync(receta.Id, new RevisionDto
			{
				Revision = receta.Revision
			});
			Assert.Equal(HttpStatusCode.Conflict, (await Assert.ThrowsAsync<ExcepcionApi>(() => cliente.CrearVersionRecetaAsync(receta.Id, new RevisionDto
			{
				Revision = revisionInicial
			}))).Estado);
			OrdenProduccionDto orden = await cliente.PlanificarProduccionAsync(new PlanificarProduccionDto
			{
				EstablecimientoId = cocina.Id,
				VersionRecetaId = receta.Id,
				CantidadPlanificada = 1000m,
				UnidadMedidaId = "unit",
				FechaPlanificada = new DateOnly(2026, 10, 15)
			});
			Assert.Equal(1000m, orden.CantidadPlanificada);
			Assert.Equal(150m, orden.Necesidades.Single((NecesidadIngredienteDto x) => x.ProductoId == carne.Id).CantidadNecesaria);
			Assert.Equal(20m, orden.Necesidades.Single((NecesidadIngredienteDto x) => x.ProductoId == queso.Id).CantidadNecesaria);
			VersionRecetaDto siguiente = await cliente.CrearVersionRecetaAsync(receta.Id, new RevisionDto
			{
				Revision = receta.Revision
			});
			Assert.Equal(receta.FamiliaRecetaId, siguiente.FamiliaRecetaId);
			Assert.True(siguiente.NumeroVersion > receta.NumeroVersion);
			Assert.False(siguiente.Publicada);
			await cliente.ActualizarRecetaAsync(siguiente.Id, new ActualizarRecetaDto
			{
				Revision = siguiente.Revision,
				Nombre = "Nueva fórmula",
				ProductoResultadoId = hamburguesa.Id,
				Rendimiento = rendimiento,
				Instrucciones = "Preparar.",
				Ingredientes = new IngredienteRecetaEntradaDto[1]
				{
					new IngredienteRecetaEntradaDto
					{
						ProductoId = carne.Id,
						UnidadMedidaId = "g",
						Cantidad = carneGramos * 2
					}
				}
			});
			OrdenProduccionDto persistida = (await cliente.ObtenerProduccionAsync()).Single((OrdenProduccionDto x) => x.Id == orden.Id);
			Assert.Equal(receta.Id, persistida.VersionRecetaId);
			Assert.Equal(150m, persistida.Necesidades.Single((NecesidadIngredienteDto x) => x.ProductoId == carne.Id).CantidadNecesaria);
			Assert.Equal(20m, persistida.Necesidades.Single((NecesidadIngredienteDto x) => x.ProductoId == queso.Id).CantidadNecesaria);
			Assert.Equal(2, (await cliente.ObtenerNecesidadesAsync()).Count((NecesidadIngredienteDto x) => x.OrdenProduccionId == orden.Id));
			Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ExcepcionApi>(() => cliente.ActualizarRecetaAsync(receta.Id, new ActualizarRecetaDto
			{
				Revision = receta.Revision,
				Nombre = "Cambio prohibido",
				ProductoResultadoId = hamburguesa.Id,
				Rendimiento = rendimiento,
				Instrucciones = "Instrucciones válidas.",
				Ingredientes = new IngredienteRecetaEntradaDto[1]
				{
					new IngredienteRecetaEntradaDto
					{
						ProductoId = carne.Id,
						UnidadMedidaId = "g",
						Cantidad = carneGramos * 2
					}
				}
			}))).Estado);
			VersionRecetaDto congelada = (await cliente.ObtenerRecetasAsync()).Single((VersionRecetaDto x) => x.Id == receta.Id);
			Assert.Equal("Hamburguesas", congelada.Nombre);
			Assert.Equal(carneGramos, congelada.Ingredientes.Single((IngredienteRecetaDto x) => x.ProductoId == carne.Id).Cantidad);
			Assert.Equal(quesoGramos, congelada.Ingredientes.Single((IngredienteRecetaDto x) => x.ProductoId == queso.Id).Cantidad);
		}
		finally
		{
			if (cliente != null)
			{
				((IDisposable)cliente).Dispose();
			}
		}
	}
}
