using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class RestriccionesRelacionalesTests
{
	[Fact]
	public void Modelo_relacional_usa_trece_tablas_de_negocio_espanolas_y_siete_tablas_identity()
	{
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			IEntityType[] entidades = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IDesignTimeModel>().Model.GetEntityTypes().ToArray();
			Assert.Equal(new string[13]
			{
				"AsignacionesCompra", "DetallesPedidoCompra", "Empresas", "Establecimientos", "IngredientesReceta", "NecesidadesIngredientes", "OrdenesProduccion", "PedidosCompra", "PresentacionesProveedor", "Productos",
				"Proveedores", "UnidadesMedida", "VersionesReceta"
			}, (from x in entidades
				// El modelo SQL registra todas estas entidades en tablas físicas.
				where !x.GetTableName()!.StartsWith("AspNet", StringComparison.Ordinal)
				select x.GetTableName()).Order(StringComparer.Ordinal));
			Assert.Equal(7, entidades.Count((IEntityType x) => x.GetTableName()!.StartsWith("AspNet", StringComparison.Ordinal)));
			foreach (IEntityType entidad in entidades.Where((IEntityType x) => !x.GetTableName()!.StartsWith("AspNet", StringComparison.Ordinal)))
			{
				StoreObjectIdentifier tabla = StoreObjectIdentifier.Table(entidad.GetTableName()!, entidad.GetSchema());
				Assert.DoesNotContain(entidad.GetProperties(), (IProperty x) =>
				{
					switch (x.GetColumnName(in tabla))
					{
					case "CompanyId":
					case "Name":
					case "Quantity":
					case "Amount":
					case "Status":
					case "CreatedBy":
					case "UnitId":
					case "FamilyId":
						return true;
					default:
						return false;
					}
				});
			}
			IEntityType usuario = entidades.Single((IEntityType x) => x.ClrType == typeof(AppUser));
			StoreObjectIdentifier tablaUsuario = StoreObjectIdentifier.Table("AspNetUsers", usuario.GetSchema());
			// AppUser declara ambas propiedades persistidas en Identity.
			Assert.Equal("CompanyId", usuario.FindProperty("EmpresaId")!.GetColumnName(in tablaUsuario));
			Assert.Equal("Enabled", usuario.FindProperty("Habilitado")!.GetColumnName(in tablaUsuario));
			Assert.False(usuario.GetKeys().Single((IKey x) => !x.IsPrimaryKey()).IsClustered());
			IEntityType ingredientes = entidades.Single((IEntityType x) => x.GetTableName() == "IngredientesReceta");
			Assert.DoesNotContain(ingredientes.GetIndexes(), (IIndex x) => x.IsUnique);
			IEntityType receta = entidades.Single((IEntityType x) => x.GetTableName() == "VersionesReceta");
			StoreObjectIdentifier tablaReceta = StoreObjectIdentifier.Table("VersionesReceta", receta.GetSchema());
			Assert.Contains(receta.GetIndexes(), (IIndex x) => x.IsUnique && x.Properties.Select((IProperty p) => p.GetColumnName(in tablaReceta)).SequenceEqual(new string[3] { "EmpresaId", "FamiliaRecetaId", "NumeroVersion" }));
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}

	[Fact]
    public void El_modelo_sql_server_declara_dieciocho_restricciones_incluido_rendimiento_opcional()
	{
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			ICheckConstraint[] restricciones = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IDesignTimeModel>().Model.GetEntityTypes().SelectMany((IEntityType x) => x.GetCheckConstraints()).ToArray();
            Assert.Equal(18, restricciones.Length);
            Assert.Equal(18, restricciones.Select((ICheckConstraint x) => x.Name).Distinct().Count());
            Assert.Contains(restricciones, x => x.Name == "CK_VersionesReceta_UnidadCantidadRendimiento");
            Assert.Contains(restricciones, x => x.Name == "CK_VersionesReceta_CantidadRendimiento");
            Assert.Contains(restricciones, x => x.Name == "CK_VersionesReceta_PorcionesRendimiento");
			Assert.DoesNotContain((IEnumerable<ICheckConstraint>)restricciones, (Predicate<ICheckConstraint>)((ICheckConstraint x) => x.Sql.Contains("Released", StringComparison.Ordinal) || x.Sql.Contains("Liberada", StringComparison.Ordinal)));
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}

	[Fact]
	public async Task Sqlite_rechaza_cantidades_invalidas_sin_depender_de_guardias_del_contexto()
	{
		HttpClient cliente;
		using (ApiFixture app = new ApiFixture())
		{
			await app.Seed();
			cliente = await app.Login("admin@test.invalid");
			try
			{
				JsonElement establecimiento = await Crear("/api/establishments", new
				{
					name = "Cocina"
				});
				JsonElement producto = await Crear("/api/products", new
				{
					name = "Producto",
					baseUnitId = "kg"
				});
				JsonElement proveedor = await Crear("/api/suppliers", new
				{
					name = "Proveedor"
				});
				JsonElement presentacion = await Crear("/api/packs", new
				{
					supplierId = Id(proveedor),
					productId = Id(producto),
					name = "Caja",
					unitId = "kg",
					quantity = 1
				});
				JsonElement receta = await Crear("/api/recipes", new
				{
					name = "Receta",
					outputProductId = Id(producto),
					yield = 1,
					instructions = "Preparar",
					ingredients = new[]
					{
						new
						{
							productId = Id(producto),
							unitId = "kg",
							quantity = 0.5m
						},
						new
						{
							productId = Id(producto),
							unitId = "kg",
							quantity = 0.5m
						}
					}
				});
				Assert.Equal(2, receta.GetProperty("ingredients").GetArrayLength());
				receta = await Crear($"/api/recipes/{Id(receta)}/publish", new
				{
					revision = Revision(receta)
				});
				JsonElement orden = await Crear("/api/production", new
				{
					establishmentId = Id(establecimiento),
					recipeVersionId = Id(receta),
					quantity = 2,
					unitId = "kg",
					date = "2026-10-14"
				});
				JsonElement necesidad = orden.GetProperty("requirements")[0];
				Assert.Equal(1, orden.GetProperty("requirements").GetArrayLength());
				Assert.Equal(2m, necesidad.GetProperty("amount").GetDecimal());
				JsonElement pedido = await Crear("/api/purchases", new
				{
					supplierId = Id(proveedor)
				});
				await Crear($"/api/purchases/{Id(pedido)}/lines", new
				{
					revision = Revision(pedido),
					supplierPackId = Id(presentacion),
					packCount = 2,
					allocations = new[]
					{
						new
						{
							requirementId = Id(necesidad),
							amount = 2,
							revision = Revision(necesidad)
						}
					}
				});
				using IServiceScope alcance = app.Services.CreateScope();
				alcance.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("company", app.Company.ToString()) }, "test"))
				};
				FoodTraceDbContext db = alcance.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
				await ((DbContext)(object)db).Database.ExecuteSqlRawAsync("UPDATE [NecesidadesIngredientes] SET [CantidadAsignada] = {0}", 0m);
				(string, string, decimal, string)[] array = new (string, string, decimal, string)[15]
				{
					("UnidadesMedida", "FactorConversion", 0m, "CK_UnidadesMedida_FactorPositivo"),
					("PresentacionesProveedor", "Cantidad", 0m, "CK_PresentacionesProveedor_CantidadPositiva"),
					("PresentacionesProveedor", "CantidadBase", -1m, "CK_PresentacionesProveedor_CantidadBasePositiva"),
					("VersionesReceta", "Rendimiento", 0m, "CK_VersionesReceta_RendimientoPositivo"),
					("IngredientesReceta", "Cantidad", -1m, "CK_IngredientesReceta_CantidadPositiva"),
					("OrdenesProduccion", "CantidadPlanificada", 0m, "CK_OrdenesProduccion_CantidadPositiva"),
					("NecesidadesIngredientes", "CantidadNecesaria", 0m, "CK_NecesidadesIngredientes_CantidadPositiva"),
					("NecesidadesIngredientes", "CantidadAsignada", -1m, "CK_NecesidadesIngredientes_AsignadaNoNegativa"),
					("NecesidadesIngredientes", "CantidadAsignada", 3m, "CK_NecesidadesIngredientes_AsignadaNoExcedeNecesidad"),
					("AsignacionesCompra", "CantidadAsignada", 0m, "CK_AsignacionesCompra_CantidadPositiva"),
					("DetallesPedidoCompra", "CantidadPresentaciones", 0m, "CK_DetallesPedidoCompra_PresentacionesPositivas"),
					("DetallesPedidoCompra", "CantidadPresentaciones", 1.5m, "CK_DetallesPedidoCompra_PresentacionesEnteras"),
					("DetallesPedidoCompra", "CantidadBasePresentacion", 0m, "CK_DetallesPedidoCompra_CantidadBasePositiva"),
					("DetallesPedidoCompra", "CantidadPedida", 0m, "CK_DetallesPedidoCompra_CantidadPedidaPositiva"),
					("DetallesPedidoCompra", "NecesidadBruta", -1m, "CK_DetallesPedidoCompra_NecesidadBrutaNoNegativa")
				};
				for (int i = 0; i < array.Length; i++)
				{
					(string, string, decimal, string) tuple = array[i];
					string tabla = tuple.Item1;
					string columna = tuple.Item2;
					decimal valor = tuple.Item3;
					string restriccion = tuple.Item4;
					string comando = string.Format(CultureInfo.InvariantCulture, "UPDATE [{0}] SET [{1}] = {{0}}", tabla, columna);
					Assert.Contains(restriccion, (await Assert.ThrowsAsync<SqliteException>(() => ((DbContext)(object)db).Database.ExecuteSqlRawAsync(comando, valor))).Message);
				}
				await ((DbContext)(object)db).Database.ExecuteSqlRawAsync("UPDATE [UnidadesMedida] SET [FactorConversion] = {0} WHERE [Id] = 'g'", 0.000001m);
				await Assert.ThrowsAsync<SqliteException>(() => ((DbContext)(object)db).Database.ExecuteSqlRawAsync("UPDATE [UnidadesMedida] SET [FactorConversion] = {0} WHERE [Id] = 'g'", 0.0m));
				await ((DbContext)(object)db).Database.ExecuteSqlRawAsync("UPDATE [DetallesPedidoCompra] SET [NecesidadBruta] = {0}", 0m);
			}
			finally
			{
				if (cliente != null)
				{
					((IDisposable)cliente).Dispose();
				}
			}
		}
		async Task<JsonElement> Crear(string ruta, object entrada)
		{
			using HttpResponseMessage respuesta = await cliente.PostAsJsonAsync(ruta, entrada);
			respuesta.EnsureSuccessStatusCode();
			return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
		}
		static Guid Id(JsonElement jsonElement)
		{
			return jsonElement.GetProperty("id").GetGuid();
		}
		static Guid Revision(JsonElement jsonElement)
		{
			return jsonElement.GetProperty("revision").GetGuid();
		}
	}
}
