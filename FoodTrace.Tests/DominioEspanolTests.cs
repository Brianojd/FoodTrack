using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class DominioEspanolTests
{
	[Fact]
	public void Tipos_miembros_y_estado_persistidos_usan_nombres_espanoles()
	{
		Assembly ensamblado = typeof(AppUser).Assembly;
		Type[] tipos = (from x in ensamblado.GetTypes()
			where x.Namespace == "FoodTrace.API.Domain.Entities"
			select x).ToArray();
		Assert.Equal(new string[15]
		{
			"AppUser", "AsignacionCompra", "DetallePedidoCompra", "Empresa", "EntidadEmpresa", "Establecimiento", "IngredienteReceta", "NecesidadIngrediente", "OrdenProduccion", "PedidoCompra",
			"PresentacionProveedor", "Producto", "Proveedor", "UnidadMedida", "VersionReceta"
		}, tipos.Select((Type x) => x.Name).Order(StringComparer.Ordinal));
		HashSet<string> nombres = new HashSet<string>(StringComparer.Ordinal)
		{
			"Id", "Revision", "Dimension", "EmpresaId", "Nombre", "UnidadBaseId", "FactorConversion", "ProveedorId", "ProductoId", "UnidadMedidaId",
			"Cantidad", "CantidadBase", "FamiliaRecetaId", "NumeroVersion", "ProductoResultadoId", "Rendimiento", "Instrucciones", "Publicada", "Ingredientes", "EstablecimientoId",
			"VersionRecetaId", "CantidadPlanificada", "FechaPlanificada", "CreadoPorId", "FechaCreacion", "Necesidades", "OrdenProduccionId", "CantidadNecesaria", "CantidadAsignada", "Estado",
			"EnviadoPorId", "FechaEnvio", "DecididoPorId", "FechaDecision", "MotivoDecision", "Detalles", "PedidoCompraId", "PresentacionProveedorId", "NombrePresentacion", "CantidadBasePresentacion",
            "CantidadPresentaciones", "CantidadPedida", "NecesidadBruta", "Asignaciones", "DetallePedidoCompraId", "NecesidadIngredienteId", "Liberada",
            "CantidadRendimiento", "UnidadRendimientoId", "PorcionesRendimiento"
		};
		foreach (Type tipo in tipos.Where((Type x) => x != typeof(AppUser)))
		{
			Assert.All(tipo.GetProperties(), (PropertyInfo propiedad) =>
			{
				Assert.Contains(propiedad.Name, nombres);
			});
			Assert.All(tipo.GetProperties(), (PropertyInfo propiedad) =>
			{
				Assert.Null(propiedad.GetCustomAttribute<JsonPropertyNameAttribute>());
			});
		}
		Type? estado = ensamblado.GetType("FoodTrace.API.Domain.Enums.EstadoPedidoCompra");
		Assert.NotNull(estado);
		Assert.Equal<string[]>(new string[4] { "Borrador", "Enviado", "Aprobado", "Rechazado" }, Enum.GetNames(estado));
		Assert.Equal(new int[4] { 0, 1, 2, 3 }, Enum.GetValues(estado).Cast<object>().Select(Convert.ToInt32));
	}

	[Fact]
	public void Modelo_traducido_conserva_columnas_facetas_revision_y_semillas_sin_propiedades_sombra()
	{
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			IModel modelo = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IDesignTimeModel>().Model;
			IEntityType[] negocio = (from x in modelo.GetEntityTypes()
				where x.ClrType.Namespace == "FoodTrace.API.Domain.Entities" && x.ClrType != typeof(AppUser)
				select x).ToArray();
			Assert.Equal(13, negocio.Length);
			IEntityType[] array = negocio;
			foreach (IEntityType entidad in array)
			{
				// Estas entidades pertenecen al modelo relacional con tablas físicas.
				StoreObjectIdentifier tabla = StoreObjectIdentifier.Table(entidad.GetTableName()!, entidad.GetSchema());
				foreach (IProperty propiedad in entidad.GetProperties())
				{
					Assert.False(propiedad.IsShadowProperty());
					Assert.Equal(propiedad.Name, propiedad.GetColumnName(in tabla));
					if (propiedad.ClrType == typeof(decimal))
					{
						Assert.Equal(18, propiedad.GetPrecision());
						Assert.Equal(6, propiedad.GetScale());
					}
				}
				if (typeof(EntidadEmpresa).IsAssignableFrom(entidad.ClrType))
				{
					// EntidadEmpresa declara Revision para todos sus tipos derivados.
					Assert.True(entidad.FindProperty("Revision")!.IsConcurrencyToken);
				}
			}
			// Tipos y propiedades registrados por FoodTraceDbContext; Id es una semilla obligatoria.
			Assert.Equal(8000, modelo.FindEntityType(typeof(VersionReceta))!.FindProperty("Instrucciones")!.GetMaxLength());
			Dictionary<string, IDictionary<string, object?>> semillas = modelo.FindEntityType(typeof(UnidadMedida))!.GetSeedData().ToDictionary((IDictionary<string, object?> x) => (string)x["Id"]!);
			Assert.Equal(new string[5] { "L", "g", "kg", "ml", "unit" }, semillas.Keys.Order(StringComparer.Ordinal));
			string[] array2 = new string[2] { "g", "ml" };
			foreach (string id in array2)
			{
				Assert.Equal(0.001m, semillas[id]["FactorConversion"]);
			}
			string[] array3 = new string[3] { "kg", "L", "unit" };
			foreach (string id2 in array3)
			{
				Assert.Equal(1m, semillas[id2]["FactorConversion"]);
			}
			PropertyInfo[] conjuntos = (from x in typeof(FoodTraceDbContext).GetProperties()
				where x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) && x.PropertyType.GenericTypeArguments[0].Namespace == "FoodTrace.API.Domain.Entities" && x.PropertyType.GenericTypeArguments[0] != typeof(AppUser)
				select x).ToArray();
			Assert.Equal(new string[13]
			{
				"AsignacionesCompra", "DetallesPedidoCompra", "Empresas", "Establecimientos", "IngredientesReceta", "NecesidadesIngredientes", "OrdenesProduccion", "PedidosCompra", "PresentacionesProveedor", "Productos",
				"Proveedores", "UnidadesMedida", "VersionesReceta"
			}, conjuntos.Select((PropertyInfo x) => x.Name).Order(StringComparer.Ordinal));
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}
}
