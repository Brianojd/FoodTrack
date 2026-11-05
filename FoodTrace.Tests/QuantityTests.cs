using FoodTrace.API.Application.Services;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public class QuantityTests
{
	[Fact]
	public void Burger_requirements_use_yield_and_base_units()
	{
		Assert.Equal(150m, Cantidades.Necesidad(150m, 0.001m, 1m, 1m, 1000m));
		Assert.Equal(20m, Cantidades.Necesidad(20m, 0.001m, 1m, 1m, 1000m));
		Assert.Equal(150m, Cantidades.Necesidad(3000m, 0.001m, 1m, 20m, 1000m));
	}

	[Fact]
	public void Conversions_require_compatible_dimensions()
	{
		Assert.Equal(1.5m, Cantidades.Convertir(1500m, "mass", 0.001m, "mass", 1m));
		Assert.Throws<ExcepcionRegla>(() => Cantidades.Convertir(1m, "mass", 1m, "volume", 1m));
	}

	[Theory]
	[InlineData(new object[] { 0 })]
	[InlineData(new object[] { -1 })]
	[InlineData(new object[] { 1000000001 })]
	public void Quantities_are_bounded(decimal quantity)
	{
		Assert.Throws<ExcepcionRegla>(() => Cantidades.Positiva(quantity));
	}

	[Fact]
	public void Requirements_round_up_to_preserve_gross_need()
	{
		Assert.Equal(0.333334m, Cantidades.Necesidad(1m, 1m, 1m, 3m, 1m));
	}

	[Fact]
	public void Packs_are_whole_and_excess_is_explicit()
	{
		Assert.Equal(160m, Cantidades.CantidadPresentaciones(8m, 20m));
		Assert.Throws<ExcepcionRegla>(() => Cantidades.CantidadPresentaciones(1.5m, 20m));
	}
}
