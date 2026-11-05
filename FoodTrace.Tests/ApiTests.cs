using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class ApiTests
{
	[Fact]
	public async Task Parallel_module_reads_use_independent_connections_in_the_isolated_sqlite_database()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		HttpClient client = await app.Login("admin@test.invalid");
		try
		{
			for (int intento = 0; intento < 10; intento++)
			{
				HttpResponseMessage[] respuestas = await Task.WhenAll(new string[5] { "/api/catalogs", "/api/recipes", "/api/production", "/api/needs", "/api/purchases" }.Select((string ruta) => client.GetAsync(ruta)));
				try
				{
					Assert.All(respuestas, (HttpResponseMessage httpResponseMessage) =>
					{
						Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);
					});
				}
				finally
				{
					HttpResponseMessage[] array = respuestas;
					foreach (HttpResponseMessage respuesta in array)
					{
						respuesta.Dispose();
					}
				}
			}
		}
		finally
		{
			if (client != null)
			{
				((IDisposable)client).Dispose();
			}
		}
	}

	private static async Task<JsonElement> Send(HttpClient client, string path, object body, HttpMethod? method = null)
	{
		using HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(method ?? HttpMethod.Post, path)
		{
			Content = JsonContent.Create(body)
		});
		bool isSuccessStatusCode = response.IsSuccessStatusCode;
		object arg = response.StatusCode;
		Assert.True(isSuccessStatusCode, $"{path}: {arg} {await response.Content.ReadAsStringAsync()}");
		return await response.Content.ReadFromJsonAsync<JsonElement>();
	}

	private static Guid Id(JsonElement element)
	{
		return element.GetProperty("id").GetGuid();
	}

	private static Guid Revision(JsonElement element)
	{
		return element.GetProperty("revision").GetGuid();
	}

	[Fact]
	public async Task Role_only_change_revokes_old_bearer_and_new_login_has_current_roles()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient admin = await app.Login("admin@test.invalid");
		using HttpClient oldBuyer = await app.Login("buyer@test.invalid");
		Assert.Equal(HttpStatusCode.OK, (await oldBuyer.GetAsync("/api/catalogs")).StatusCode);
		string? buyerId = (await admin.GetFromJsonAsync<JsonElement>("/api/users")).EnumerateArray().Single((JsonElement x) => x.GetProperty("email").GetString() == "buyer@test.invalid").GetProperty("id")
			.GetString();
		Assert.True((await Send(admin, "/api/users/" + buyerId, new
		{
			enabled = true,
			roles = new string[1] { "admin" }
		}, HttpMethod.Put)).GetProperty("enabled").GetBoolean());
		Assert.Equal(HttpStatusCode.Unauthorized, (await oldBuyer.GetAsync("/api/catalogs")).StatusCode);
		using HttpClient current = await app.Login("buyer@test.invalid");
		JsonElement identity = await current.GetFromJsonAsync<JsonElement>("/api/me");
		Assert.Equal<string[]>(new string[1] { "admin" }, (from x in identity.GetProperty("roles").EnumerateArray()
			select x.GetString()).ToArray());
		Assert.Equal(HttpStatusCode.Forbidden, (await current.PostAsJsonAsync("/api/purchases", new
		{
			supplierId = Guid.NewGuid()
		})).StatusCode);
	}

	[Fact]
	public async Task Bearer_login_enforces_tenant_roles_and_disablement()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient anonymous = app.CreateClient();
		Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/catalogs")).StatusCode);
		Assert.False((await anonymous.PostAsJsonAsync("/register", new
		{
			email = "x",
			password = "x"
		})).IsSuccessStatusCode);
		using HttpClient admin = await app.Login("admin@test.invalid");
		using HttpClient other = await app.Login("other@test.invalid");
		using HttpClient buyer = await app.Login("buyer@test.invalid");
		JsonElement product = await Send(admin, "/api/products", new
		{
			name = "Beef",
			baseUnitId = "kg"
		});
		Assert.Empty((await other.GetFromJsonAsync<JsonElement>("/api/catalogs")).GetProperty("products").EnumerateArray());
		Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsJsonAsync("/api/products", new
		{
			name = "No",
			baseUnitId = "kg"
		})).StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsJsonAsync("/api/recipes", new
		{
			name = "Invalid",
			outputProductId = Id(product),
			yield = 1,
			instructions = "Cook",
			ingredients = new[]
			{
				new
				{
					productId = Id(product),
					unitId = "kg",
					quantity = 1
				}
			}
		})).StatusCode);
		string? buyerId = (await admin.GetFromJsonAsync<JsonElement>("/api/users")).EnumerateArray().Single((JsonElement x) => x.GetProperty("email").GetString() == "buyer@test.invalid").GetProperty("id")
			.GetString();
		await Send(admin, "/api/users/" + buyerId, new
		{
			enabled = false,
			roles = new string[1] { "buyer" }
		}, HttpMethod.Put);
		Assert.Equal(HttpStatusCode.Unauthorized, (await buyer.GetAsync("/api/catalogs")).StatusCode);
	}

	[Fact]
	public async Task Http_workflow_snapshots_needs_allocates_packs_and_approves_with_conflicts()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		using HttpClient buyer = await app.Login("buyer@test.invalid");
		JsonElement site = await Send(client, "/api/establishments", new
		{
			name = "Kitchen"
		});
		JsonElement beef = await Send(client, "/api/products", new
		{
			name = "Beef",
			baseUnitId = "kg"
		});
		JsonElement cheese = await Send(client, "/api/products", new
		{
			name = "Cheese",
			baseUnitId = "kg"
		});
		JsonElement burger = await Send(client, "/api/products", new
		{
			name = "Burger",
			baseUnitId = "unit"
		});
		JsonElement supplier = await Send(client, "/api/suppliers", new
		{
			name = "Supplier"
		});
		JsonElement pack = await Send(client, "/api/packs", new
		{
			supplierId = Id(supplier),
			productId = Id(beef),
			name = "20 kg box",
			unitId = "kg",
			quantity = 20
		});
		JsonElement recipe = await Send(client, "/api/recipes", new
		{
			name = "Burger",
			outputProductId = Id(burger),
			yield = 20,
			instructions = "Cook and assemble",
			ingredients = new[]
			{
				new
				{
					productId = Id(beef),
					unitId = "g",
					quantity = 3000
				},
				new
				{
					productId = Id(cheese),
					unitId = "g",
					quantity = 400
				}
			}
		});
		recipe = await Send(client, $"/api/recipes/{Id(recipe)}/publish", new
		{
			revision = Revision(recipe)
		});
		JsonElement[] needs = (await Send(client, "/api/production", new
		{
			establishmentId = Id(site),
			recipeVersionId = Id(recipe),
			quantity = 1000,
			unitId = "unit",
			date = "2026-10-12"
		})).GetProperty("requirements").EnumerateArray().ToArray();
		JsonElement need = needs.Single((JsonElement x) => x.GetProperty("productId").GetGuid() == Id(beef));
		Assert.Equal(150m, need.GetProperty("amount").GetDecimal());
		Assert.Equal(20m, needs.Single((JsonElement x) => x.GetProperty("productId").GetGuid() == Id(cheese)).GetProperty("amount").GetDecimal());
		JsonElement purchase = await Send(client, "/api/purchases", new
		{
			supplierId = Id(supplier)
		});
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new
		{
			revision = Revision(purchase),
			supplierPackId = Id(pack),
			packCount = 8,
			allocations = new[]
			{
				new
				{
					requirementId = Id(need),
					amount = 150,
					revision = Revision(need)
				}
			}
		});
		JsonElement line = purchase.GetProperty("lines")[0];
		Assert.Equal(160m, line.GetProperty("orderedAmount").GetDecimal());
		Assert.Equal(150m, line.GetProperty("grossNeed").GetDecimal());
		JsonElement competing = await Send(client, "/api/purchases", new
		{
			supplierId = Id(supplier)
		});
		Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/purchases/{Id(competing)}/lines", new
		{
			revision = Revision(competing),
			supplierPackId = Id(pack),
			packCount = 8,
			allocations = new[]
			{
				new
				{
					requirementId = Id(need),
					amount = 150,
					revision = Revision(need)
				}
			}
		})).StatusCode);
		Guid draftRevision = Revision(purchase);
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new
		{
			revision = draftRevision
		});
		Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/approve", new
		{
			revision = Revision(purchase)
		})).StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/purchases/{Id(purchase)}/approve", new
		{
			revision = draftRevision
		})).StatusCode);
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/approve", new
		{
			revision = Revision(purchase)
		});
		Assert.NotEqual(JsonValueKind.Null, purchase.GetProperty("decidedAt").ValueKind);
		Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/purchases/{Id(purchase)}/lines/{Id(line)}", new
		{
			revision = Revision(purchase),
			packCount = 9
		})).StatusCode);
		JsonElement draft = await Send(client, $"/api/recipes/{Id(recipe)}/versions", new
		{
			revision = Revision(recipe)
		});
		Assert.False(draft.GetProperty("published").GetBoolean());
		Assert.Equal(2, draft.GetProperty("number").GetInt32());
		using IServiceScope scope = app.Services.CreateScope();
		IHttpContextAccessor accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
		accessor.HttpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("company", app.Company.ToString()) }, "test"))
		};
		FoodTraceDbContext db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
		VersionReceta tracked = await db.VersionesReceta.SingleAsync((VersionReceta x) => x.Id == Id(recipe));
		tracked.Publicada = false;
		tracked.Rendimiento = 4m;
		await Assert.ThrowsAsync<ExcepcionRegla>(() => ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken)));
		((DbContext)(object)db).ChangeTracker.Clear();
		db.DetallesPedidoCompra.Add(new DetallePedidoCompra
		{
			EmpresaId = app.Company,
			PedidoCompraId = Id(purchase),
			PresentacionProveedorId = Id(pack),
			ProductoId = Id(beef),
			UnidadBaseId = "kg",
			NombrePresentacion = "Injected",
			CantidadPresentaciones = 1m,
			CantidadBasePresentacion = 20m,
			CantidadPedida = 20m,
			NecesidadBruta = 20m
		});
		await Assert.ThrowsAsync<ExcepcionRegla>(() => ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken)));
		((DbContext)(object)db).ChangeTracker.Clear();
		db.IngredientesReceta.Add(new IngredienteReceta
		{
			EmpresaId = app.Company,
			VersionRecetaId = Id(recipe),
			ProductoId = Id(beef),
			UnidadMedidaId = "g",
			Cantidad = 1m
		});
		await Assert.ThrowsAsync<ExcepcionRegla>(() => ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken)));
		((DbContext)(object)db).ChangeTracker.Clear();
		(await db.NecesidadesIngredientes.SingleAsync((NecesidadIngrediente x) => x.Id == Id(need))).CantidadNecesaria = 1m;
		await Assert.ThrowsAsync<ExcepcionRegla>(() => ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken)));
	}

	[Fact]
	public async Task Rejection_releases_allocations_and_draft_edits_require_current_revision()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		JsonElement site = await Send(client, "/api/establishments", new
		{
			name = "Kitchen"
		});
		JsonElement product = await Send(client, "/api/products", new
		{
			name = "Milk",
			baseUnitId = "L"
		});
		JsonElement supplier = await Send(client, "/api/suppliers", new
		{
			name = "Supplier"
		});
		JsonElement pack = await Send(client, "/api/packs", new
		{
			supplierId = Id(supplier),
			productId = Id(product),
			name = "2 L",
			unitId = "ml",
			quantity = 2000
		});
		JsonElement recipe = await Send(client, "/api/recipes", new
		{
			name = "Milk batch",
			outputProductId = Id(product),
			yield = 1,
			instructions = "Pour",
			ingredients = new[]
			{
				new
				{
					productId = Id(product),
					unitId = "ml",
					quantity = 1000
				}
			}
		});
		Guid originalRevision = Revision(recipe);
		recipe = await Send(client, $"/api/recipes/{Id(recipe)}", new
		{
			name = "Edited draft",
			outputProductId = Id(product),
			yield = 1,
			instructions = "Pour carefully",
			ingredients = new[]
			{
				new
				{
					productId = Id(product),
					unitId = "ml",
					quantity = 1000
				}
			},
			revision = originalRevision
		}, HttpMethod.Put);
		Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/recipes/{Id(recipe)}/publish", new
		{
			revision = originalRevision
		})).StatusCode);
		recipe = await Send(client, $"/api/recipes/{Id(recipe)}/publish", new
		{
			revision = Revision(recipe)
		});
		JsonElement need = (await Send(client, "/api/production", new
		{
			establishmentId = Id(site),
			recipeVersionId = Id(recipe),
			quantity = 3,
			unitId = "L",
			date = "2026-10-12"
		})).GetProperty("requirements")[0];
		JsonElement purchase = await Send(client, "/api/purchases", new
		{
			supplierId = Id(supplier)
		});
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new
		{
			revision = Revision(purchase),
			supplierPackId = Id(pack),
			packCount = 2,
			allocations = new[]
			{
				new
				{
					requirementId = Id(need),
					amount = 3,
					revision = Revision(need)
				}
			}
		});
		Guid oldRevision = Revision(purchase);
		JsonElement line = purchase.GetProperty("lines")[0];
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines/{Id(line)}", new
		{
			revision = oldRevision,
			packCount = 3
		}, HttpMethod.Put);
		Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/purchases/{Id(purchase)}/lines/{Id(line)}", new
		{
			revision = oldRevision,
			packCount = 4
		})).StatusCode);
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines/{Id(line)}/remove", new
		{
			revision = Revision(purchase)
		});
		JsonElement releasedNeed = (await client.GetFromJsonAsync<JsonElement>("/api/needs"))[0];
		Assert.Equal(0m, releasedNeed.GetProperty("allocated").GetDecimal());
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/lines", new
		{
			revision = Revision(purchase),
			supplierPackId = Id(pack),
			packCount = 2,
			allocations = new[]
			{
				new
				{
					requirementId = Id(releasedNeed),
					amount = 3,
					revision = Revision(releasedNeed)
				}
			}
		});
		purchase = await Send(client, $"/api/purchases/{Id(purchase)}/submit", new
		{
			revision = Revision(purchase)
		});
		Assert.True((await Send(client, $"/api/purchases/{Id(purchase)}/reject", new
		{
			revision = Revision(purchase),
			reason = "Choose another supplier"
		})).GetProperty("lines")[0].GetProperty("allocations")[0].GetProperty("released").GetBoolean());
		JsonElement updated = (await client.GetFromJsonAsync<JsonElement>("/api/needs"))[0];
		Assert.Equal(0m, updated.GetProperty("allocated").GetDecimal());
		Assert.Equal(3m, updated.GetProperty("amount").GetDecimal());
	}

	[Fact]
	public async Task Relational_tenant_foreign_keys_and_immutable_units_protect_direct_saves()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient other = await app.Login("other@test.invalid");
		JsonElement otherProduct = await Send(other, "/api/products", new
		{
			name = "Other",
			baseUnitId = "kg"
		});
		using IServiceScope scope = app.Services.CreateScope();
		scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("company", app.Company.ToString()) }, "test"))
		};
		FoodTraceDbContext db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
		Proveedor supplier = new Proveedor
		{
			EmpresaId = app.Company,
			Nombre = "Supplier"
		};
		db.Proveedores.Add(supplier);
		await ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken));
		db.PresentacionesProveedor.Add(new PresentacionProveedor
		{
			EmpresaId = app.Company,
			ProveedorId = supplier.Id,
			ProductoId = Id(otherProduct),
			Nombre = "Invalid",
			UnidadMedidaId = "kg",
			Cantidad = 1m,
			CantidadBase = 1m
		});
		await Assert.ThrowsAsync<DbUpdateException>(() => ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken)));
		((DbContext)(object)db).ChangeTracker.Clear();
		(await db.UnidadesMedida.SingleAsync((UnidadMedida x) => x.Id == "g")).FactorConversion = 1m;
		await Assert.ThrowsAsync<ExcepcionRegla>(() => ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken)));
	}

	[Fact]
	public async Task Persisted_revision_rejects_two_writers_that_loaded_the_same_draft()
	{
		using ApiFixture app = new ApiFixture();
		await app.Seed();
		using HttpClient client = await app.Login("admin@test.invalid");
		JsonElement purchase = await Send(client, "/api/purchases", new
		{
			supplierId = Id(await Send(client, "/api/suppliers", new
			{
				name = "Supplier"
			}))
		});
		using IServiceScope firstScope = app.Services.CreateScope();
		using IServiceScope secondScope = app.Services.CreateScope();
		firstScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("company", app.Company.ToString()) }, "test"))
		};
		FoodTraceDbContext first = firstScope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
		FoodTraceDbContext second = secondScope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
		PedidoCompra a = await first.PedidosCompra.SingleAsync((PedidoCompra x) => x.Id == Id(purchase));
		PedidoCompra b = await second.PedidosCompra.SingleAsync((PedidoCompra x) => x.Id == Id(purchase));
		a.Revision = Guid.NewGuid();
		b.Revision = Guid.NewGuid();
		await ((DbContext)(object)first).SaveChangesAsync(default(CancellationToken));
		await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ((DbContext)(object)second).SaveChangesAsync(default(CancellationToken)));
	}

	[Fact]
	public async Task Bootstrap_is_transactional_idempotent_and_grants_only_explicit_admin_role()
	{
		ApiFixture app = new ApiFixture();
		try
		{
			await app.Seed();
			Guid company = Guid.NewGuid();
			IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Bootstrap:CompanyId"] = company.ToString(),
				["Bootstrap:CompanyName"] = "Bootstrap company",
				["Bootstrap:AdminEmail"] = "bootstrap@test.invalid",
				["Bootstrap:AdminPassword"] = "Local-test-only!42"
			}).Build();
			await InicializadorIdentidad.EjecutarAsync(app.Services, configuration);
			await InicializadorIdentidad.EjecutarAsync(app.Services, configuration);
			using IServiceScope scope = app.Services.CreateScope();
			UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
			AppUser? user = await users.FindByEmailAsync("bootstrap@test.invalid");
			Assert.NotNull(user);
			Assert.Equal(company, user.EmpresaId);
			Assert.True(await users.CheckPasswordAsync(user, "Local-test-only!42"));
			Assert.NotEqual("Local-test-only!42", user.PasswordHash);
			IList<string> expected = new string[1] { "admin" };
			Assert.Equal(expected, await users.GetRolesAsync(user));
			configuration["Bootstrap:CompanyId"] = app.OtherCompany.ToString();
			await Assert.ThrowsAsync<InvalidOperationException>(() => InicializadorIdentidad.EjecutarAsync(app.Services, configuration));
			Assert.Equal(3, await scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>().Empresas.CountAsync());
		}
		finally
		{
			if (app != null)
			{
				((IDisposable)app).Dispose();
			}
		}
	}
}
