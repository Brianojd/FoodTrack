using System;
using System.Collections.Generic;
using FoodTrace.API.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class ConfiguracionTests
{
	[Theory]
	[InlineData(null)]
	[InlineData(new object[] { "" })]
	[InlineData(new object[] { "   " })]
	public void Conexion_ausente_o_vacia_falla_claramente_sin_valor_alternativo(string? valor)
	{
		IConfigurationRoot configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:FoodTrace"] = valor }).Build();
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => ConfiguracionFoodTrace.ObtenerConexion(configuracion));
		Assert.Equal("Falta configurar ConnectionStrings:FoodTrace.", error.Message);
	}

	[Fact]
	public void Conexion_configurada_se_preserva_sin_reescribir_destino()
	{
		IConfigurationRoot configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:FoodTrace"] = "Server=synthetic.invalid;Database=ContractOnly;Integrated Security=true" }).Build();
		Assert.Equal("Server=synthetic.invalid;Database=ContractOnly;Integrated Security=true", ConfiguracionFoodTrace.ObtenerConexion(configuracion));
	}
}
