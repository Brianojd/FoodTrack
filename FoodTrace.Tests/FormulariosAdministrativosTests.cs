using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.UnidadesMedida;
using FoodTrace.Models.Administracion;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class FormulariosAdministrativosTests
{
	[Theory]
	[InlineData(new object[] { "es-AR" })]
	[InlineData(new object[] { "en-US" })]
	public void El_contenido_se_valida_independientemente_de_la_cultura_del_circuito(string cultura)
	{
		CultureInfo anterior = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
			FormularioPresentacion modelo = new FormularioPresentacion
			{
				Nombre = "Bolsa",
				ProveedorId = Guid.NewGuid(),
				ProductoId = Guid.NewGuid(),
				UnidadMedidaId = "kg",
				Cantidad = 1.5m
			};
			Assert.True(Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true));
			modelo.Cantidad = 0.0000001m;
			Assert.False(Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true));
		}
		finally
		{
			CultureInfo.CurrentCulture = anterior;
		}
	}

	[Fact]
	public void La_presentacion_exige_proveedor_producto_unidad_nombre_y_contenido_positivo()
	{
		FormularioPresentacion modelo = new FormularioPresentacion
		{
			Cantidad = 0m
		};
		List<ValidationResult> errores = new List<ValidationResult>();
		Assert.False(Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true));
		Assert.Contains((IEnumerable<ValidationResult>)errores, (Predicate<ValidationResult>)((ValidationResult x) => x.MemberNames.Contains("Nombre")));
		Assert.Contains((IEnumerable<ValidationResult>)errores, (Predicate<ValidationResult>)((ValidationResult x) => x.MemberNames.Contains("UnidadMedidaId")));
		Assert.Contains((IEnumerable<ValidationResult>)errores, (Predicate<ValidationResult>)((ValidationResult x) => x.MemberNames.Contains("Cantidad")));
		modelo.Nombre = "Bolsa";
		modelo.UnidadMedidaId = "kg";
		modelo.Cantidad = 1m;
		errores.Clear();
		Assert.False(Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true));
		Assert.Contains((IEnumerable<ValidationResult>)errores, (Predicate<ValidationResult>)((ValidationResult x) => x.MemberNames.Contains("ProveedorId")));
		Assert.Contains((IEnumerable<ValidationResult>)errores, (Predicate<ValidationResult>)((ValidationResult x) => x.MemberNames.Contains("ProductoId")));
		modelo.ProveedorId = Guid.NewGuid();
		modelo.ProductoId = Guid.NewGuid();
		Assert.True(Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), validateAllProperties: true));
	}

	[Fact]
	public void Las_unidades_se_filtran_por_dimension_sin_calcular_cantidades_oficiales()
	{
		Guid producto = Guid.NewGuid();
		CatalogosDto catalogosDto = new CatalogosDto();
		int num = 1;
		List<ProductoDto> list = new List<ProductoDto>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = new ProductoDto
		{
			Id = producto,
			UnidadBaseId = "kg"
		};
		catalogosDto.Productos = list;
		num = 3;
		List<UnidadMedidaDto> list2 = new List<UnidadMedidaDto>(num);
		CollectionsMarshal.SetCount(list2, num);
		Span<UnidadMedidaDto> span = CollectionsMarshal.AsSpan(list2);
		span[0] = new UnidadMedidaDto
		{
			Id = "kg",
			Dimension = "mass"
		};
		span[1] = new UnidadMedidaDto
		{
			Id = "g",
			Dimension = "mass"
		};
		span[2] = new UnidadMedidaDto
		{
			Id = "L",
			Dimension = "volume"
		};
		catalogosDto.UnidadesMedida = list2;
		CatalogosDto catalogos = catalogosDto;
		Assert.Equal(new string[2] { "kg", "g" }, from x in CompatibilidadUnidades.ParaProducto(catalogos, producto)
			select x.Id);
		Assert.Empty(CompatibilidadUnidades.ParaProducto(catalogos, Guid.NewGuid()));
		catalogos.Productos[0].UnidadBaseId = "desconocida";
		Assert.Empty(CompatibilidadUnidades.ParaProducto(catalogos, producto));
	}

	[Fact]
	public void El_acceso_y_la_creacion_de_usuario_rechazan_formularios_vacios_y_correos_invalidos()
	{
		FormularioIngreso ingreso = new FormularioIngreso();
		Assert.False(Validator.TryValidateObject(ingreso, new ValidationContext(ingreso), new List<ValidationResult>(), validateAllProperties: true));
		ingreso.Correo = "incorrecto";
		ingreso.Contrasena = "solo-prueba";
		Assert.False(Validator.TryValidateObject(ingreso, new ValidationContext(ingreso), new List<ValidationResult>(), validateAllProperties: true));
		ingreso.Correo = "cuenta@example.invalid";
		Assert.True(Validator.TryValidateObject(ingreso, new ValidationContext(ingreso), new List<ValidationResult>(), validateAllProperties: true));
		FormularioUsuario usuario = new FormularioUsuario
		{
			Correo = ingreso.Correo,
			Contrasena = ingreso.Contrasena
		};
		Assert.False(Validator.TryValidateObject(usuario, new ValidationContext(usuario), new List<ValidationResult>(), validateAllProperties: true));
		usuario.Roles = new string[1] { "buyer" };
		Assert.True(Validator.TryValidateObject(usuario, new ValidationContext(usuario), new List<ValidationResult>(), validateAllProperties: true));
	}
}
