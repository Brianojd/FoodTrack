using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FoodTrace.Components.Layout;
using FoodTrace.Components.Pages.Acceso;
using FoodTrace.Components.Shared;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Utilities;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class RuntimeAccesibilidadTests
{
	[Theory]
	[InlineData(new object[] { "Success" })]
	[InlineData(new object[] { "Error" })]
	[InlineData(new object[] { "Info" })]
	public void Los_colores_semanticos_verificados_tienen_contraste_para_texto_normal(string nombre)
	{
		Palette paleta = TemaFoodTrace.Tema.PaletteLight;
		if (1 == 0)
		{
		}
		MudColor mudColor;
		if (nombre == "Error")
		{
			mudColor = paleta.Error;
		}
		else
		{
			mudColor = ((!(nombre == "Info")) ? paleta.Success : paleta.Info);
		}
		if (1 == 0)
		{
		}
		MudColor color = mudColor;
		double[] texto = new double[3]
		{
			(int)color.R,
			(int)color.G,
			(int)color.B
		};
		double[] fondo = texto.Select((double canal) => 229.5 + canal * 0.1).ToArray();
		double contraste = (Luminancia(fondo) + 0.05) / (Luminancia(texto) + 0.05);
		Assert.True(contraste >= 4.5, $"El contraste del color semantico es {contraste:F2}:1; debe alcanzar 4.5:1.");
		static double Luminancia(double[] canales)
		{
			double[] lineales = (from canal in canales
				select canal / 255.0 into canal
				select (canal <= 0.04045) ? (canal / 12.92) : Math.Pow((canal + 0.055) / 1.055, 2.4)).ToArray();
			return lineales[0] * 0.2126 + lineales[1] * 0.7152 + lineales[2] * 0.0722;
		}
	}

	[Theory]
	[InlineData(new object[] { false })]
	[InlineData(new object[] { true })]
	public async Task Carga_y_acceso_exponen_su_nombre_accesible(bool acceso)
	{
		using ClienteFoodTraceApi cliente = new ClienteFoodTraceApi(new HttpClient
		{
			BaseAddress = new Uri("https://example.invalid")
		});
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		await using ServiceProvider servicios = ServiciosRender.Crear(cliente, estado);
		await using AsyncServiceScope scope = servicios.CreateAsyncScope();
		HtmlRenderer renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		try
		{
			await renderer.Dispatcher.InvokeAsync(async () =>
			{
				if (acceso)
				{
					HtmlRootComponent cabecera = await renderer.RenderComponentAsync<HeadOutlet>();
					await renderer.RenderComponentAsync<FormularioAcceso>();
					Assert.Contains("<title>Acceso", cabecera.ToHtmlString());
				}
				else
				{
					Assert.Contains("aria-label=\"Comprobando acceso\"", (await renderer.RenderComponentAsync<EstadoCarga>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Mensaje"] = "Comprobando acceso" }))).ToHtmlString());
				}
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
}
