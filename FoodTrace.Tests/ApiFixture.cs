using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.API.Controllers;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Reconstructed with ILSpy 11.1.0.9782 from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class ApiFixture : WebApplicationFactory<AutenticacionController>
{
	private readonly SqliteConnection connection = new SqliteConnection(new SqliteConnectionStringBuilder
	{
		DataSource = "foodtrace-test-" + Guid.NewGuid().ToString("N"),
		Mode = SqliteOpenMode.Memory,
		Cache = SqliteCacheMode.Shared,
		Pooling = false
	}.ToString());

	public const string Password = "Local-test-only!42";

	public Guid Company { get; } = Guid.NewGuid();

	public Guid OtherCompany { get; } = Guid.NewGuid();

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		connection.Open();
		builder.UseEnvironment("Development");
		builder.ConfigureServices((IServiceCollection services) =>
		{
			services.RemoveAll<DbContextOptions<FoodTraceDbContext>>();
			services.RemoveAll<IDbContextOptionsConfiguration<FoodTraceDbContext>>();
			services.AddDbContext<FoodTraceDbContext>((Action<DbContextOptionsBuilder>?)((DbContextOptionsBuilder options) =>
			{
				options.UseSqlite(connection.ConnectionString);
			}), ServiceLifetime.Scoped, ServiceLifetime.Scoped);
			services.AddDataProtection().UseEphemeralDataProtectionProvider();
		});
	}

	public async Task Seed()
	{
		UserManager<AppUser> users;
		using (IServiceScope scope = Services.CreateScope())
		{
			FoodTraceDbContext db = scope.ServiceProvider.GetRequiredService<FoodTraceDbContext>();
			await ((DbContext)(object)db).Database.EnsureCreatedAsync();
			db.Empresas.AddRange(new Empresa
			{
				Id = Company,
				Nombre = "First company"
			}, new Empresa
			{
				Id = OtherCompany,
				Nombre = "Other company"
			});
			await ((DbContext)(object)db).SaveChangesAsync(default(CancellationToken));
			RoleManager<IdentityRole> roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			string[] todos = Roles.Todos;
			foreach (string role in todos)
			{
				InicializadorIdentidad.ComprobarResultado(await roles.CreateAsync(new IdentityRole(role)));
			}
			users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
			await User("admin@test.invalid", Company, Roles.Todos);
			await User("buyer@test.invalid", Company, new string[1] { "buyer" });
			await User("other@test.invalid", OtherCompany, Roles.Todos);
		}
		async Task User(string email, Guid company, string[] assigned)
		{
			AppUser user = new AppUser
			{
				UserName = email,
				Email = email,
				EmpresaId = company
			};
			InicializadorIdentidad.ComprobarResultado(await users.CreateAsync(user, "Local-test-only!42"));
			InicializadorIdentidad.ComprobarResultado(await users.AddToRolesAsync(user, assigned));
		}
	}

	public async Task<HttpClient> Login(string email)
	{
		HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		});
		HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new
		{
			email = email,
			password = "Local-test-only!42"
		});
		response.EnsureSuccessStatusCode();
		string? token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return client;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			connection.Dispose();
		}
	}
}
