using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Dtos.Autenticacion;
using FoodTrace.API.Dtos.Comunes;
using FoodTrace.API.Dtos.Recetas;
using FoodTrace.API.Dtos.Usuarios;
using FoodTrace.API.Dtos.Weather;
using FoodTrace.Services.Api;
using FoodTrace.Tests.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class DtoConventionTests
{
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

	[Fact]
	public void Dtos_de_negocio_usan_modulos_y_tipos_espanoles()
	{
		string[] modulos = new string[11]
		{
			"Autenticacion", "Comunes", "Empresas", "Establecimientos", "Productos", "UnidadesMedida", "Proveedores", "Usuarios", "Recetas", "Produccion",
			"Compras"
		};
		string[] nombres = new string[36]
		{
			"IniciarSesionDto", "UsuarioActualDto", "RespuestaInicioSesionDto", "CatalogosDto", "ErrorDto", "EntidadConNombreDto", "EntidadEmpresaDto", "NombreDto", "RevisionDto", "EmpresaDto",
			"ActualizarEmpresaDto", "CrearEstablecimientoDto", "ProductoDto", "CrearProductoDto", "UnidadMedidaDto", "PresentacionProveedorDto", "CrearPresentacionProveedorDto", "CrearProveedorDto", "UsuarioDto", "CrearUsuarioDto",
			"ActualizarMembresiaDto", "CrearRecetaDto", "ActualizarRecetaDto", "IngredienteRecetaEntradaDto", "VersionRecetaDto", "IngredienteRecetaDto", "OrdenProduccionDto", "PlanificarProduccionDto", "NecesidadIngredienteDto", "PedidoCompraDto",
			"CrearPedidoCompraDto", "DetallePedidoCompraDto", "AsignacionCompraDto", "AgregarDetalleCompraDto", "AsignacionNecesidadEntradaDto", "ActualizarCantidadPresentacionesDto"
		};
		HashSet<string> miembros = new HashSet<string>(StringComparer.Ordinal)
		{
			"Id", "Revision", "Dimension", "Roles", "Nombre", "EmpresaId", "Correo", "Contrasena", "Habilitado", "Motivo",
			"Mensaje", "TipoToken", "TokenAcceso", "ExpiraEn", "TokenRenovacion", "Empresa", "UnidadesMedida", "Establecimientos", "Productos", "Proveedores",
			"Presentaciones", "UnidadBaseId", "FactorConversion", "ProveedorId", "ProductoId", "UnidadMedidaId", "Cantidad", "CantidadBase", "FamiliaRecetaId", "NumeroVersion",
			"ProductoResultadoId", "Rendimiento", "Instrucciones", "Publicada", "Ingredientes", "EstablecimientoId", "VersionRecetaId", "CantidadPlanificada", "FechaPlanificada", "CreadoPorId",
			"FechaCreacion", "Necesidades", "OrdenProduccionId", "CantidadNecesaria", "CantidadAsignada", "Estado", "EnviadoPorId", "FechaEnvio", "DecididoPorId", "FechaDecision",
			"MotivoDecision", "Detalles", "PedidoCompraId", "PresentacionProveedorId", "NombrePresentacion", "CantidadBasePresentacion", "CantidadPresentaciones", "CantidadPedida", "NecesidadBruta", "Asignaciones",
			"DetallePedidoCompraId", "NecesidadIngredienteId", "Liberada", "CantidadRendimiento", "UnidadRendimientoId", "PorcionesRendimiento", "PorPorciones",
			"VistaParcial"
		};
		Assembly[] array = new Assembly[2]
		{
			typeof(MapeadorDtos).Assembly,
			typeof(ClienteFoodTraceApi).Assembly
		};
		foreach (Assembly ensamblado in array)
		{
			foreach (Type tipo in ensamblado.GetTypes().Where((Type x) =>
			{
				string? text = x.Namespace;
				return text != null && text.Contains(".Dtos.") && !text.EndsWith(".Weather", StringComparison.Ordinal);
			}))
			{
				// El filtro anterior exige un namespace de DTO no nulo.
				Assert.Contains(tipo.Namespace!.Split('.').Last(), (IEnumerable<string>)modulos);
				Assert.Contains(tipo.Name, (IEnumerable<string>)nombres);
				Assert.All(tipo.GetProperties(), (PropertyInfo propiedad) =>
				{
					Assert.Contains(propiedad.Name, miembros);
				});
			}
		}
	}

	[Fact]
	public void Every_transport_property_explicitly_freezes_its_json_name_including_inherited_members()
	{
		Assembly[] array = new Assembly[2]
		{
			typeof(MapeadorDtos).Assembly,
			typeof(ClienteFoodTraceApi).Assembly
		};
		foreach (Assembly assembly in array)
		{
			foreach (Type type in from t in assembly.GetTypes()
				where t.Namespace?.Contains(".Dtos.") ?? false
				select t)
			{
				PropertyInfo[] properties = type.GetProperties();
				foreach (PropertyInfo property in properties)
				{
					JsonPropertyNameAttribute? attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
					Assert.True(attribute != null && !string.IsNullOrWhiteSpace(attribute.Name), type.FullName + "." + property.Name + " must explicitly preserve its JSON name.");
				}
			}
		}
	}

	[Fact]
	public void Entrada_de_ingrediente_traducida_conserva_json_y_metadatos_de_validacion()
	{
		IngredienteRecetaEntradaDto entrada = new IngredienteRecetaEntradaDto
		{
			ProductoId = Guid.Empty,
			UnidadMedidaId = "g",
			Cantidad = 150m
		};
		JsonElement json = JsonSerializer.SerializeToElement(entrada, Options);
		Assert.Equal(new string[3] { "productId", "unitId", "quantity" }, from x in json.EnumerateObject()
			select x.Name);
		Assert.Equal(150m, json.GetProperty("quantity").GetDecimal());
		Assert.Null(entrada.GetType().GetProperty("ProductId"));
		Assert.Null(typeof(MapeadorDtos).Assembly.GetType("FoodTrace.API.Dtos.Recipes.RecipeIngredientInputDto"));
		using ApiFixture app = new ApiFixture();
		ModelMetadata metadatos = app.Services.GetRequiredService<IModelMetadataProvider>().GetMetadataForProperty(entrada.GetType(), "UnidadMedidaId");
		Assert.Equal("UnitId", metadatos.GetDisplayName());
	}

	[Fact]
	public void Responses_match_frozen_pre_conversion_json()
	{
		string[] buffer = new string[5];
		buffer[0] = RepositoryRoot();
		buffer[1] = "FoodTrace.Tests";
		buffer[2] = "Fixtures";
		buffer[3] = "Dtos";
		buffer[4] = "original-responses.json";
		// El fixture congelado validado por SHA-256 es un objeto con cada entrada histórica.
		JsonNode root = JsonNode.Parse(File.ReadAllText(Path.Combine(buffer)))!;
		foreach (MethodInfo method in from m in typeof(MapeadorDtos).GetMethods()
			where m.Name == "ADto"
			select m)
		{
			Type type = method.GetParameters()[0].ParameterType;
			bool[] array = new bool[2] { false, true };
			foreach (bool populated in array)
			{
				object entity = EntitySample(type, populated);
				// ADto tiene retorno no nulo; el fixture contiene default/populated por entidad.
				object dto = method.Invoke(null, new object[1] { entity })!;
                var esperado = root[MapaContratoHistorico.NombreFixture(type)]![populated ? "populated" : "default"]!.DeepClone();
                if (type == typeof(FoodTrace.API.Domain.Entities.VersionReceta))
                {
                    // Extensión aprobada: mantener cada campo histórico y exigir los tres nuevos, sin tocar el fixture congelado.
                    esperado["yieldQuantity"] = populated ? JsonValue.Create(12.5m) : null;
                    esperado["yieldUnitId"] = populated ? JsonValue.Create("YieldUnitId") : null;
                    esperado["yieldPortions"] = populated ? JsonValue.Create(12.5m) : null;
                }
                if (type == typeof(FoodTrace.API.Domain.Entities.PedidoCompra))
                {
                    // Extensión aprobada: indicador aditivo de vista parcial para planificadores.
                    esperado["partialView"] = false;
                }
                Assert.True(JsonNode.DeepEquals(esperado, JsonSerializer.SerializeToNode(dto, dto.GetType(), Options)), type.Name + " frozen response changed.");
			}
		}
		Assert.True(JsonNode.DeepEquals(root["WeatherForecast"], JsonSerializer.SerializeToNode(new WeatherForecast
		{
			Date = new DateOnly(2026, 10, 12),
			TemperatureC = -7,
			TemperatureF = 20,
			Summary = null
		}, Options)));
		Assert.True(JsonNode.DeepEquals(root["MeDto"], JsonSerializer.SerializeToNode(new UsuarioActualDto
		{
			Id = "synthetic-user",
			EmpresaId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
			Correo = null,
			Roles = new string[1] { "buyer" }
		}, Options)));
		Assert.True(JsonNode.DeepEquals(root["UserDto"], JsonSerializer.SerializeToNode(new UsuarioDto
		{
			Id = "synthetic-user",
			Correo = null,
			Habilitado = true,
			Roles = new List<string> { "buyer" }
		}, Options)));
        // ErrorDto ya fue retirado por la transición previa a ProblemDetails; no recrear el DTO obsoleto para compilar esta prueba.
        Assert.Null(typeof(MapeadorDtos).Assembly.GetType("FoodTrace.API.Dtos.Comunes.ErrorDto"));
        Assert.Null(typeof(ClienteFoodTraceApi).Assembly.GetType("FoodTrace.Dtos.Comunes.ErrorDto"));
        Assert.True(JsonNode.DeepEquals(root["ErrorDto"], JsonNode.Parse("{\"message\":\"Synthetic error\"}")));
	}

	private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
	{
		DirectoryInfo? directory = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
		while (directory != null && !File.Exists(Path.Combine(directory.FullName, "FoodTrace.slnx")))
		{
			directory = directory.Parent;
		}
		return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
	}

	[Fact]
	public void Transport_types_are_mutable_initialized_classes_in_same_named_module_files()
	{
		Assembly[] array = new Assembly[2]
		{
			typeof(MapeadorDtos).Assembly,
			typeof(ClienteFoodTraceApi).Assembly
		};
		foreach (Assembly assembly in array)
		{
			Type[] types = (from t in assembly.GetTypes()
				where t.Namespace?.Contains(".Dtos.") ?? false
				select t).ToArray();
			Assert.NotEmpty(types);
			Assert.Equal((assembly == typeof(MapeadorDtos).Assembly) ? 35 : 32, types.Length);
			Assert.DoesNotContain((IEnumerable<Type>)assembly.GetTypes(), (Predicate<Type>)((Type t) => t.Namespace?.Contains(".Contracts.") ?? false));
			Type[] array2 = types;
			foreach (Type type in array2)
			{
				Assert.True(type.IsClass);
				Assert.Null(type.GetMethod("<Clone>$"));
				string project = ((assembly == typeof(MapeadorDtos).Assembly) ? "FoodTrace.API" : "FoodTrace");
				// El filtro de types solo admite namespaces que contienen .Dtos.
				string module = type.Namespace!.Split('.').Last();
				Assert.Contains(module, (IEnumerable<string>)new string[12]
				{
					"Autenticacion", "Comunes", "Empresas", "Establecimientos", "Productos", "UnidadesMedida", "Proveedores", "Usuarios", "Recetas", "Produccion",
					"Compras", "Weather"
				});
				string[] buffer = new string[5];
				buffer[0] = RepositoryRoot();
				buffer[1] = project;
				buffer[2] = "Dtos";
				buffer[3] = module;
				buffer[4] = type.Name + ".cs";
				string path = Path.Combine(buffer);
				Assert.True(File.Exists(path), path);
				string source = File.ReadAllText(path);
				Assert.DoesNotContain(" record ", source);
				Assert.DoesNotContain("required ", source);
				Assert.Single(Regex.Matches(source, "\\bclass\\s+\\w+"));
				Assert.Single(types, (Type candidate) => candidate.Name == type.Name);
				Assert.All(type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public), (PropertyInfo propertyInfo) =>
				{
					Assert.Contains(propertyInfo.Name + " { get; set; }", source);
				});
				Assert.All(type.GetProperties(), (PropertyInfo propertyInfo) =>
				{
					MethodInfo? getMethod = propertyInfo.GetMethod;
					Assert.True((object?)getMethod != null && getMethod.IsPublic && (propertyInfo.SetMethod?.IsPublic ?? false));
					// La aserción anterior exige explícitamente un setter público no nulo.
					Assert.DoesNotContain((IEnumerable<Type>)propertyInfo.SetMethod!.ReturnParameter.GetRequiredCustomModifiers(), (Predicate<Type>)((Type modifier) => modifier.Name == "IsExternalInit"));
				});
				if (type.IsAbstract)
				{
					continue;
				}
				// Los DTO concretos tienen constructor público sin argumentos.
				object instance = Activator.CreateInstance(type)!;
				NullabilityInfoContext nullability = new NullabilityInfoContext();
				foreach (PropertyInfo property in from p in type.GetProperties()
					where !p.PropertyType.IsValueType
					select p)
				{
					if (nullability.Create(property).ReadState == NullabilityState.NotNull)
					{
						Assert.NotNull(property.GetValue(instance));
					}
				}
			}
			// Las referencias del ensamblado contienen nombres completos.
			Assert.DoesNotContain((IEnumerable<AssemblyName>)assembly.GetReferencedAssemblies(), (Predicate<AssemblyName>)((AssemblyName a) => assembly.GetName().Name == "FoodTrace" && (a.Name == "FoodTrace.API" || a.Name!.Contains("EntityFramework"))));
			string projectDirectory = Path.Combine(RepositoryRoot(), (assembly == typeof(MapeadorDtos).Assembly) ? "FoodTrace.API" : "FoodTrace");
			Assert.Equal(types.Length, Directory.GetFiles(Path.Combine(projectDirectory, "Dtos"), "*.cs", SearchOption.AllDirectories).Length);
			string oldDirectory = Path.Combine(projectDirectory, "Contracts");
			if (Directory.Exists(oldDirectory))
			{
				Assert.Empty(Directory.GetFiles(oldDirectory, "*.cs", SearchOption.AllDirectories));
			}
		}
	}

	[Fact]
	public void Public_application_boundaries_expose_dtos_not_persistence_or_loose_commands()
	{
		Assembly api = typeof(MapeadorDtos).Assembly;
		foreach (Type type in from t in api.GetTypes()
			where t.Namespace == "FoodTrace.API.Application.Interfaces"
			select t)
		{
			MethodInfo[] methods = type.GetMethods();
			foreach (MethodInfo method in methods)
			{
				AssertDtoResponse(method.ReturnType);
				Assert.All(method.GetParameters(), (ParameterInfo parameter) =>
				{
					Assert.True(parameter.ParameterType == typeof(Guid) || parameter.ParameterType == typeof(Guid?) || parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(CancellationToken) || (parameter.ParameterType.Namespace?.StartsWith("FoodTrace.API.Dtos.") ?? false));
				});
				Assert.DoesNotContain((IEnumerable<ParameterInfo>)method.GetParameters(), (Predicate<ParameterInfo>)((ParameterInfo p) =>
				{
					switch (p.Name)
					{
					case "revision":
					case "action":
					case "packCount":
					case "quantity":
					case "date":
						return true;
					default:
						return false;
					}
				}));
				Assert.Contains((IEnumerable<ParameterInfo>)method.GetParameters(), (Predicate<ParameterInfo>)((ParameterInfo p) => p.ParameterType == typeof(CancellationToken) && p.HasDefaultValue));
			}
		}
	}

	private static void AssertDtoResponse(Type type)
	{
		if (type.IsGenericType)
		{
			Assert.All(type.GetGenericArguments(), AssertDtoResponse);
			return;
		}
		Assert.StartsWith("FoodTrace.API.Dtos.", type.Namespace);
		AssertDtoProperties(type, new HashSet<Type>());
	}

	private static void AssertDtoProperties(Type type, HashSet<Type> visited)
	{
		if (!visited.Add(type))
		{
			return;
		}
		Assert.DoesNotContain("Domain", type.Namespace ?? "");
		Assert.DoesNotContain("Infrastructure", type.Namespace ?? "");
		if (type.IsArray)
		{
			// IsArray garantiza que GetElementType devuelve el tipo del elemento.
			AssertDtoProperties(type.GetElementType()!, visited);
			return;
		}
		if (type.IsGenericType)
		{
			Type[] genericArguments = type.GetGenericArguments();
			foreach (Type argument in genericArguments)
			{
				AssertDtoProperties(argument, visited);
			}
			return;
		}
		string? text = type.Namespace;
		if (text != null && text.StartsWith("FoodTrace.API.Dtos."))
		{
			PropertyInfo[] properties = type.GetProperties();
			foreach (PropertyInfo property in properties)
			{
				AssertDtoProperties(property.PropertyType, visited);
			}
		}
	}

	[Fact]
	public void Concrete_use_cases_and_typed_web_operations_have_dto_response_boundaries()
	{
		Type[] array = new Type[3]
		{
			typeof(ServicioFlujo),
			typeof(ServicioCatalogos),
			typeof(ServicioUsuarios)
		};
		foreach (Type type in array)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo method in methods)
			{
				AssertDtoResponse(method.ReturnType);
			}
		}
		foreach (MethodInfo method2 in from m in typeof(ClienteFoodTraceApi).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
			where m.Name.EndsWith("Async", StringComparison.Ordinal)
			select m)
		{
			Assert.False(method2.IsGenericMethod);
			Type result = Assert.Single(method2.ReturnType.GetGenericArguments());
			if (result.IsGenericType)
			{
				result = Assert.Single(result.GetGenericArguments());
			}
			Assert.StartsWith("FoodTrace.Dtos.", result.Namespace);
			Assert.Contains((IEnumerable<ParameterInfo>)method2.GetParameters(), (Predicate<ParameterInfo>)((ParameterInfo p) => p.ParameterType == typeof(CancellationToken) && p.HasDefaultValue));
			Assert.DoesNotContain((IEnumerable<ParameterInfo>)method2.GetParameters(), (Predicate<ParameterInfo>)((ParameterInfo p) =>
			{
				bool flag = p.ParameterType == typeof(object);
				bool flag2 = flag;
				if (!flag2)
				{
					bool flag3;
					switch (p.Name)
					{
					case "path":
					case "action":
					case "revision":
						flag3 = true;
						break;
					default:
						flag3 = false;
						break;
					}
					flag2 = flag3;
				}
				return flag2;
			}));
		}
		string[] array2 = new string[7] { "Establecimientos", "Productos", "Proveedores", "Usuarios", "Recetas", "Produccion", "Compras" };
		foreach (string module in array2)
		{
			string[] buffer = new string[6];
			buffer[0] = RepositoryRoot();
			buffer[1] = "FoodTrace";
			buffer[2] = "Components";
			buffer[3] = "Pages";
			buffer[4] = module;
			buffer[5] = module + ".razor";
			string path = Path.Combine(buffer);
			string source = File.ReadAllText(path);
			if (File.Exists(path + ".cs"))
			{
				source += File.ReadAllText(path + ".cs");
			}
			Assert.DoesNotContain("new {", source);
			Assert.DoesNotContain("Api.Post<", source);
			Assert.DoesNotContain("Api.Put<", source);
		}
	}

	[Fact]
	public async Task Other_missing_and_null_reference_inputs_are_http_validation_failures()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		(string, string)[] missingCases = new (string, string)[11]
		{
			("/auth/login", "{\"password\":\"synthetic\"}"),
			("/api/products", "{\"baseUnitId\":\"kg\"}"),
			("/api/products", "{\"name\":\"Product\"}"),
			("/api/establishments", "{}"),
			("/api/suppliers", "{}"),
			("/api/packs", "{\"name\":\"Pack\"}"),
			("/api/users", "{\"email\":\"synthetic@example.invalid\",\"password\":\"synthetic\"}"),
			("/api/production", "{}"),
			($"/api/purchases/{Guid.Empty}/lines", "{}"),
			("/api/recipes", "{\"name\":\"Recipe\",\"instructions\":\"Cook\",\"ingredients\":[{\"quantity\":1}]}"),
			("/api/recipes", "{\"instructions\":\"Cook\",\"ingredients\":[]}")
		};
		(string, string)[] array = missingCases;
		JsonElement value;
		for (int i = 0; i < array.Length; i++)
		{
			var (path, json) = array[i];
			using JsonDocument body = JsonDocument.Parse(json);
			using HttpResponseMessage missing = await client.PostAsJsonAsync(path, body.RootElement);
			Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
			Assert.True((await missing.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out value));
		}
		(string, object)[] array2 = new (string, object)[4]
		{
			("/api/products", new
			{
				name = (string?)null,
				baseUnitId = "kg"
			}),
			("/api/users", new
			{
				email = "synthetic@example.invalid",
				password = "synthetic",
				roles = (string[]?)null
			}),
			($"/api/purchases/{Guid.Empty}/lines", new
			{
				allocations = (object?)null
			}),
			("/api/recipes", new
			{
				name = "Recipe",
				instructions = "Cook",
				ingredients = new[]
				{
					new
					{
						unitId = (string?)null,
						quantity = 1
					}
				}
			})
		};
		for (int j = 0; j < array2.Length; j++)
		{
			var (path2, body2) = array2[j];
			using HttpResponseMessage response = await client.PostAsJsonAsync(path2, body2);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out value));
		}
		using HttpResponseMessage omittedMembership = await client.PutAsJsonAsync($"/api/users/{Guid.Empty}", new
		{
			enabled = true
		});
		Assert.Equal(HttpStatusCode.BadRequest, omittedMembership.StatusCode);
	}

	[Fact]
	public async Task Explicit_empty_ingredients_are_rejected_and_weather_keeps_computed_wire_value()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		using HttpResponseMessage productResponse = await client.PostAsJsonAsync("/api/products", new
		{
			name = "Output",
			baseUnitId = "unit"
		});
		productResponse.EnsureSuccessStatusCode();
		using HttpResponseMessage recipe = await client.PostAsJsonAsync("/api/recipes", new
		{
			name = "Empty draft",
			outputProductId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(),
			yield = 1,
			instructions = "Cook",
			ingredients = Array.Empty<object>()
		});
		Assert.Equal(HttpStatusCode.BadRequest, recipe.StatusCode);
		Assert.Equal("The Ingredients field is required.", Assert.Single((await recipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("Ingredients").EnumerateArray()).GetString());
		JsonElement weather = await client.GetFromJsonAsync<JsonElement>("/WeatherForecast");
		Assert.Equal(5, weather.GetArrayLength());
		Assert.All(weather.EnumerateArray(), (JsonElement item) =>
		{
			Assert.Equal(32 + (int)((double)item.GetProperty("temperatureC").GetInt32() / 0.5556), item.GetProperty("temperatureF").GetInt32());
		});
	}

	[Fact]
	public async Task Empty_required_roles_are_http_validation_failures_and_direct_line_allows_empty_allocations()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		JsonElement supplier = await client.PostAsJsonAsync("/api/suppliers", new { name = "Synthetic" }).Result.Content.ReadFromJsonAsync<JsonElement>();
		JsonElement product = await client.PostAsJsonAsync("/api/products", new { name = "Synthetic", baseUnitId = "kg" }).Result.Content.ReadFromJsonAsync<JsonElement>();
		JsonElement pack = await client.PostAsJsonAsync("/api/packs", new { supplierId = supplier.GetProperty("id").GetGuid(), productId = product.GetProperty("id").GetGuid(), name = "1 kg", unitId = "kg", quantity = 1 }).Result.Content.ReadFromJsonAsync<JsonElement>();
		JsonElement purchase = await client.PostAsJsonAsync("/api/purchases", new { supplierId = supplier.GetProperty("id").GetGuid() }).Result.Content.ReadFromJsonAsync<JsonElement>();
		using HttpResponseMessage createUser = await client.PostAsJsonAsync("/api/users", new
		{
			email = "synthetic@example.invalid",
			password = "Local-test-only!42",
			roles = Array.Empty<string>()
		});
		using HttpResponseMessage updateUser = await client.PutAsJsonAsync($"/api/users/{Guid.Empty}", new
		{
			enabled = true,
			roles = Array.Empty<string>()
		});
		using HttpResponseMessage line = await client.PostAsJsonAsync($"/api/purchases/{Guid.Empty}/lines", new
		{
			allocations = Array.Empty<object>()
		});
		HttpResponseMessage[] array = new HttpResponseMessage[2] { createUser, updateUser };
		foreach (HttpResponseMessage response in array)
		{
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out var _));
		}
		using HttpResponseMessage direct = await client.PostAsJsonAsync($"/api/purchases/{purchase.GetProperty("id").GetGuid()}/lines", new
		{
			revision = purchase.GetProperty("revision").GetGuid(),
			supplierPackId = pack.GetProperty("id").GetGuid(),
			packCount = 2,
			allocations = Array.Empty<object>()
		});
		direct.EnsureSuccessStatusCode();
		JsonElement directLine = (await direct.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("lines")[0];
		Assert.Equal(0m, directLine.GetProperty("grossNeed").GetDecimal());
		Assert.Empty(directLine.GetProperty("allocations").EnumerateArray());
	}

	[Theory]
	[InlineData(new object[] { "" })]
	[InlineData(new object[] { "   " })]
	public async Task Required_string_inputs_reject_empty_and_whitespace_before_use_case_execution(string name)
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		using HttpResponseMessage response = await client.PostAsJsonAsync("/api/products", new
		{
			name = name,
			baseUnitId = "kg"
		});
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out var _));
	}

	private static object EntitySample(Type type, bool populated)
	{
		// El helper solo recibe entidades concretas del mapa histórico, con constructor público.
		object value = Activator.CreateInstance(type)!;
		foreach (PropertyInfo property in from p in type.GetProperties()
			where p.CanWrite
			select p)
		{
			Type scalar = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
			if (scalar == typeof(Guid))
			{
				if (populated || (object?)Nullable.GetUnderlyingType(property.PropertyType) == null)
				{
					property.SetValue(value, Guid.Parse("11111111-1111-1111-1111-111111111111"));
				}
			}
			else if (populated)
			{
                string nombreHistorico = type == typeof(FoodTrace.API.Domain.Entities.VersionReceta) ? property.Name switch
                {
                    "CantidadRendimiento" => "YieldQuantity", "UnidadRendimientoId" => "YieldUnitId", "PorcionesRendimiento" => "YieldPortions",
                    _ => MapaContratoHistorico.NombreValorSintetico(type, property.Name)
                } : MapaContratoHistorico.NombreValorSintetico(type, property.Name);
				object? input;
				if (scalar == typeof(string))
				{
					input = nombreHistorico;
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
					// scalar se ha comprobado como List<T>, cuyo constructor crea una lista no nula.
					IList list = (IList)Activator.CreateInstance(scalar)!;
					list.Add(EntitySample(scalar.GetGenericArguments()[0], populated: true));
					input = list;
				}
				if (input != null)
				{
					property.SetValue(value, input);
				}
			}
		}
		return value;
	}

	[Fact]
	public async Task Missing_and_null_recipe_ingredients_are_rejected_before_use_case_execution()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		using HttpResponseMessage productResponse = await client.PostAsJsonAsync("/api/products", new
		{
			name = "Output",
			baseUnitId = "unit"
		});
		productResponse.EnsureSuccessStatusCode();
		Guid id = (await productResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
		object[] array = new object[2]
		{
			new
			{
				name = "Recipe",
				outputProductId = id,
				yield = 1,
				instructions = "Cook"
			},
			new
			{
				name = "Recipe",
				outputProductId = id,
				yield = 1,
				instructions = "Cook",
				ingredients = (object?)null
			}
		};
		foreach (object payload in array)
		{
			JsonElement errors;
			using (HttpResponseMessage response = await client.PostAsJsonAsync("/api/recipes", payload))
			{
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
				Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out errors));
				JsonProperty error = Assert.Single(errors.EnumerateObject());
				Assert.Equal("Ingredients", error.Name);
				Assert.Equal("The Ingredients field is required.", Assert.Single(error.Value.EnumerateArray()).GetString());
			}
			errors = default;
		}
		Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/recipes")).EnumerateArray());
		using HttpResponseMessage nested = await client.PostAsJsonAsync("/api/recipes", new
		{
			name = "Recipe",
			outputProductId = id,
			yield = 1,
			instructions = "Cook",
			ingredients = new[]
			{
				new
				{
					productId = id,
					quantity = 1
				}
			}
		});
		Assert.Equal(HttpStatusCode.BadRequest, nested.StatusCode);
		Assert.Equal("The UnitId field is required.", Assert.Single((await nested.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("Ingredients[0].UnitId").EnumerateArray()).GetString());
	}

	[Fact]
	public void Direct_deserialization_uses_initialized_defaults_without_changing_nullable_revision()
	{
		CrearRecetaDto? dto = JsonSerializer.Deserialize<CrearRecetaDto>("{}", new JsonSerializerOptions(JsonSerializerDefaults.Web));
		Assert.NotNull(dto);
		Assert.Empty(dto.Nombre);
		Assert.Empty(dto.Instrucciones);
		Assert.Empty(dto.Ingredientes);
		Assert.Null(dto.Revision);
	}
}
