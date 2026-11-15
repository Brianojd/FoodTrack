using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests.Support;

internal static class ServiciosRender
{
	private sealed class JsSinNavegador : IJSRuntime
	{
		// El doble sin JavaScript devuelve default intencionadamente, también para referencias.
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			return ValueTask.FromResult<TValue>(default!);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			return ValueTask.FromResult<TValue>(default!);
		}
	}

	private sealed class NavegacionPrueba : NavigationManager
	{
		public NavegacionPrueba()
		{
			Initialize("https://localhost/", "https://localhost/");
		}

		protected override void NavigateToCore(string uri, bool forceLoad)
		{
		}
	}

	public static ServiceProvider Crear(ClienteFoodTraceApi cliente, EstadoAdministracion estado)
	{
		IServiceCollection servicios = new ServiceCollection().AddLogging();
		servicios.AddSingleton(cliente);
		servicios.AddSingleton((ISesionFoodTrace)cliente);
		servicios.AddSingleton((ICatalogosFoodTrace)cliente);
		servicios.AddSingleton((IUsuariosFoodTrace)cliente);
		servicios.AddSingleton((IServicioRecetas)cliente);
		servicios.AddSingleton((IServicioProduccion)cliente);
		servicios.AddSingleton((IServicioCompras)cliente);
		servicios.AddSingleton(estado);
		servicios.AddSingleton<IJSRuntime, JsSinNavegador>();
		servicios.AddSingleton<NavigationManager, NavegacionPrueba>();
		servicios.AddMudServices();
		return servicios.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateScopes = true
		});
	}
}
