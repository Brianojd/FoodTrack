using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodTrace.API.Controllers;
using FoodTrace.API.Infrastructure.Persistence;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class StructureTests
{
	private static readonly Assembly Api = typeof(AutenticacionController).Assembly;

	private static readonly Assembly Web = typeof(CatalogosDto).Assembly;

	[Fact]
	public void Api_boundaries_have_real_registered_services_and_no_controller_persistence()
	{
		using ApiFixture app = new ApiFixture();
		using IServiceScope scope = app.Services.CreateScope();
		string[] array = new string[3] { "IServicioFlujo", "IServicioCatalogos", "IServicioUsuarios" };
		foreach (string name in array)
		{
			Type? contract = Api.GetType("FoodTrace.API.Application.Interfaces." + name);
			Assert.NotNull(contract);
			Assert.True(contract.IsInterface);
			object service = scope.ServiceProvider.GetRequiredService(contract);
			Assert.Equal("FoodTrace.API.Application.Services", service.GetType().Namespace);
		}
		ConstructorInfo controller = typeof(FlujoController).GetConstructors().Single();
		Assert.DoesNotContain((IEnumerable<ParameterInfo>)controller.GetParameters(), (Predicate<ParameterInfo>)((ParameterInfo p) =>
		{
			string name2 = p.ParameterType.Name;
			return (name2 == "FoodDb" || name2 == "FoodTraceDbContext") ? true : false;
		}));
		Type[] entidadesConReglas = Api.GetTypes().Where((Type t) =>
		{
			string name2 = t.Name;
			return (name2 == "VersionReceta" || name2 == "PedidoCompra") ? true : false;
		}).ToArray();
		Assert.Equal(2, entidadesConReglas.Length);
		Type[] array2 = entidadesConReglas;
		foreach (Type type in array2)
		{
			Assert.Equal("FoodTrace.API.Domain.Entities", type.Namespace);
			Assert.All(type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public), (MethodInfo method) =>
			{
				Assert.True(method.IsSpecialName, "Entity rules belong in Application services.");
			});
		}
	}

	[Fact]
	public void Persistence_and_migration_metadata_use_current_full_names()
	{
		Assert.NotNull(Api.GetType("FoodTrace.API.Infrastructure.Persistence.FoodTraceDbContext"));
		Assert.NotNull(Api.GetType("FoodTrace.API.Infrastructure.Persistence.FoodTraceDbContextFactory"));
		Assert.NotNull(Api.GetType("FoodTrace.API.Infrastructure.Persistence.Migrations.FoodTraceDbContextModelSnapshot"));
		Assert.DoesNotContain((IEnumerable<Type>)Api.GetTypes(), (Predicate<Type>)((Type t) =>
		{
			switch (t.Namespace)
			{
			case "FoodTrace.API.Data":
			case "FoodTrace.API.Security":
			case "FoodTrace.API.Domain":
				return true;
			default:
				return false;
			}
		}));
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			IMigrationsAssembly migrations = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IMigrationsAssembly>();
			KeyValuePair<string, TypeInfo> entry = Assert.Single(migrations.Migrations);
			Assert.Equal("20261015141456_Inicial", entry.Key);
			string[] currentNames = (from t in ((DbContext)(object)db).Model.GetEntityTypes()
				select t.Name).Order().ToArray();
			// La migración Inicial registrada incluye su snapshot y proveedor relacional.
			Assert.Equal<string[]>(currentNames, (from t in migrations.ModelSnapshot!.Model.GetEntityTypes()
				select t.Name).Order().ToArray());
			Migration migration = migrations.CreateMigration(entry.Value, ((DbContext)(object)db).Database.ProviderName!);
			Assert.Equal<string[]>(currentNames, (from t in migration.TargetModel.GetEntityTypes()
				select t.Name).Order().ToArray());
			Assert.All(from t in Api.GetTypes()
				where t.Namespace == "FoodTrace.API.Domain.Entities"
				select t, (Type t) =>
			{
				Assert.Single(Api.GetTypes(), (Type candidate) => candidate.Name == t.Name);
			});
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}

	[Fact]
	public void Explicit_response_projections_preserve_every_existing_entity_json_field()
	{
		Type? mapper = Api.GetType("FoodTrace.API.Application.Services.MapeadorDtos");
		Assert.NotNull(mapper);
		Type[] types = MapaContratoHistorico.TiposEntidad;
		Assert.Equal(13, types.Length);
		Type[] array = types;
		// Campos aditivos autorizados en el DTO que no provienen de la entidad.
		var propiedadesAditivas = new Dictionary<Type, string[]>
		{
			[typeof(FoodTrace.API.Domain.Entities.PedidoCompra)] = new[] { "VistaParcial" }
		};
		foreach (Type type in array)
		{
			object entity = Sample(type);
			MethodInfo method = mapper.GetMethods().Single((MethodInfo m) => m.Name == "ADto" && m.GetParameters()[0].ParameterType == type);
			// Las proyecciones ADto devuelven DTO no nulos para entidades concretas.
			object dto = method.Invoke(null, new object[1] { entity })!;
			Assert.StartsWith("FoodTrace.API.Dtos.", dto.GetType().Namespace);
			JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
			var dtoProps = dto.GetType().GetProperties().Select((PropertyInfo p) => p.Name).ToHashSet();
			var entityProps = type.GetProperties().Select((PropertyInfo p) => p.Name).ToHashSet();
			Assert.True(entityProps.IsSubsetOf(dtoProps), type.Name + " DTO omite campos de la entidad.");
			if (propiedadesAditivas.TryGetValue(type, out var aditivas))
			{
				var extras = dtoProps.Except(entityProps).ToHashSet();
				Assert.True(extras.SetEquals(aditivas), type.Name + " tiene campos aditivos no autorizados.");
			}
			else
			{
				Assert.Equal(entityProps.Count, dtoProps.Count);
			}
			JsonNode esperado = MapaContratoHistorico.SerializarEntidad(entity, type);
			JsonNode actual = JsonSerializer.SerializeToNode(dto, dto.GetType(), options)!;
			if (type == typeof(FoodTrace.API.Domain.Entities.PedidoCompra))
			{
				esperado["partialView"] = false;
			}
			Assert.True(JsonNode.DeepEquals(esperado, actual), type.Name + " JSON shape changed.");
			object empty = Activator.CreateInstance(type)!;
			object emptyDto = method.Invoke(null, new object[1] { empty })!;
			JsonNode esperadoEmpty = MapaContratoHistorico.SerializarEntidad(empty, type);
			JsonNode actualEmpty = JsonSerializer.SerializeToNode(emptyDto, emptyDto.GetType(), options)!;
			if (type == typeof(FoodTrace.API.Domain.Entities.PedidoCompra))
			{
				esperadoEmpty["partialView"] = false;
			}
			Assert.True(JsonNode.DeepEquals(esperadoEmpty, actualEmpty), type.Name + " default JSON shape changed.");
		}
	}

	private static object Sample(Type type)
	{
		// El mapa solo contiene entidades concretas con constructor público sin argumentos.
		object value = Activator.CreateInstance(type)!;
		foreach (PropertyInfo property in from p in type.GetProperties()
			where p.CanWrite
			select p)
		{
			Type scalar = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
			object? input;
			if (scalar == typeof(string))
			{
				input = MapaContratoHistorico.NombreValorSintetico(type, property.Name);
			}
			else if (scalar == typeof(Guid))
			{
				input = Guid.NewGuid();
			}
			else if (scalar == typeof(decimal))
			{
				input = 12.5m;
			}
			else if (scalar == typeof(int))
			{
				input = 2;
			}
			else if (scalar == typeof(bool))
			{
				input = true;
			}
			else if (scalar == typeof(DateOnly))
			{
				input = new DateOnly(2026, 10, 12);
			}
			else if (scalar == typeof(DateTimeOffset))
			{
				input = new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
			}
			else
			{
				input = (scalar.IsEnum ? Enum.ToObject(scalar, 2) : null);
			}
			if (scalar.IsGenericType && scalar.GetGenericTypeDefinition() == typeof(List<>))
			{
				// List<T> dispone de constructor sin argumentos y nunca produce una instancia nula.
				IList list = (IList)Activator.CreateInstance(scalar)!;
				list.Add(Sample(scalar.GetGenericArguments()[0]));
				input = list;
			}
			if (input != null)
			{
				property.SetValue(value, input);
			}
		}
		return value;
	}

	[Fact]
	public void Web_modules_use_independent_contracts_and_parent_owned_form_state()
	{
		// GetReferencedAssemblies devuelve nombres de referencias completos.
		Assert.DoesNotContain((IEnumerable<AssemblyName>)Web.GetReferencedAssemblies(), (Predicate<AssemblyName>)((AssemblyName a) => a.Name == "FoodTrace.API" || a.Name!.Contains("EntityFramework") || a.Name.Contains("SqlClient")));
		string[] array = new string[7] { "Establecimientos", "Productos", "Proveedores", "Usuarios", "Recetas", "Produccion", "Compras" };
		foreach (string module in array)
		{
			Assert.NotNull(Web.GetType("FoodTrace.Components.Pages." + module + "." + module));
		}
		string[] array2 = new string[7] { "Administracion.FormularioNombre", "Administracion.FormularioProducto", "Administracion.FormularioPresentacion", "Administracion.FormularioUsuario", "Recetas.ModeloVersionReceta", "Produccion.ModeloPlanificacion", "Compras.ModeloDetallePedido" };
		foreach (string model in array2)
		{
			Assert.NotNull(Web.GetType("FoodTrace.Models." + model));
		}
		string[] array3 = new string[4] { "EstadoAdministracion", "EstadoRecetas", "EstadoProduccion", "EstadoCompras" };
		foreach (string state in array3)
		{
			Assert.NotNull(Web.GetType("FoodTrace.Services.Estado." + state));
		}
		Assert.NotNull(Web.GetType("FoodTrace.Services.Api.ClienteFoodTraceApi"));
		Assert.NotNull(Web.GetType("FoodTrace.Components.Shared.AvisosFlujo"));
		Assert.NotNull(Web.GetType("FoodTrace.Components.Shared.RegistroFlujo"));
		string[] array4 = new string[5] { "EncabezadoPagina", "EstadoCarga", "EstadoVacio", "MensajeError", "DialogoConfirmacion" };
		foreach (string shared in array4)
		{
			Assert.NotNull(Web.GetType("FoodTrace.Components.Shared." + shared));
		}
		Assert.DoesNotContain((IEnumerable<Type>)Web.GetTypes(), (Predicate<Type>)((Type t) =>
		{
			string? text = t.Namespace;
			return (text == "FoodTrace.Contracts" || text == "FoodTrace.Services") ? true : false;
		}));
	}
}
