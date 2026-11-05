using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Proveedores;
using FoodTrace.Models.Compras;
using FoodTrace.Services.Estado;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class ComprasTests
{
	[Fact]
	public void El_detalle_exige_presentaciones_enteras_permite_compra_directa_y_mantiene_asignaciones_validadas()
	{
		Guid proveedor = Guid.NewGuid();
		Guid producto = Guid.NewGuid();
		Guid pack = Guid.NewGuid();
		Guid necesidadId = Guid.NewGuid();
		Guid revision = Guid.NewGuid();
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
		num = 1;
		List<PresentacionProveedorDto> list2 = new List<PresentacionProveedorDto>(num);
		CollectionsMarshal.SetCount(list2, num);
		CollectionsMarshal.AsSpan(list2)[0] = new PresentacionProveedorDto
		{
			Id = pack,
			ProveedorId = proveedor,
			ProductoId = producto,
			CantidadBase = 10m
		};
		catalogosDto.Presentaciones = list2;
		CatalogosDto catalogos = catalogosDto;
		PedidoCompraDto pedido = new PedidoCompraDto
		{
			Id = Guid.NewGuid(),
			Revision = Guid.NewGuid(),
			ProveedorId = proveedor
		};
		List<NecesidadIngredienteDto> necesidades = new List<NecesidadIngredienteDto>
		{
			new NecesidadIngredienteDto
			{
				Id = necesidadId,
				ProductoId = producto,
				Revision = revision,
				UnidadBaseId = "kg",
				CantidadNecesaria = 20m,
				CantidadAsignada = 5m
			}
		};
		ModeloDetallePedido modelo = new ModeloDetallePedido
		{
			PresentacionId = pack,
			CantidadPresentaciones = 1.5m
		};
		modelo.Asignaciones[necesidadId] = new ModeloAsignacionCompra
		{
			NecesidadId = necesidadId,
			Revision = revision,
			Cantidad = 10m
		};
		Assert.NotEmpty(modelo.Validar(pedido, catalogos, necesidades));
		modelo.CantidadPresentaciones = 1m;
		Assert.Empty(modelo.Validar(pedido, catalogos, necesidades));
		AgregarDetalleCompraDto entrada = modelo.ParaEnviar(pedido.Revision);
		Assert.Equal(pedido.Revision, entrada.Revision);
		Assert.Equal(revision, Assert.Single(entrada.Asignaciones).Revision);
		modelo.Asignaciones[necesidadId].Cantidad = 11m;
		Assert.NotEmpty(modelo.Validar(pedido, catalogos, necesidades));
		modelo.CantidadPresentaciones = 2m;
		modelo.Asignaciones[necesidadId].Cantidad = 16m;
		Assert.NotEmpty(modelo.Validar(pedido, catalogos, necesidades));
		modelo.Asignaciones[necesidadId].Cantidad = 0m;
		Assert.NotEmpty(modelo.Validar(pedido, catalogos, necesidades));
		modelo.Asignaciones.Clear();
		modelo.CantidadPresentaciones = 2m;
		Assert.Empty(modelo.Validar(pedido, catalogos, necesidades));
		entrada = modelo.ParaEnviar(pedido.Revision);
		Assert.Empty(entrada.Asignaciones);
		Assert.Equal(2m, entrada.CantidadPresentaciones);
	}

	[Fact]
	public void Actualizar_la_lista_no_reemplaza_revision_seleccionada_ni_formularios()
	{
		PedidoCompraDto pedido = new PedidoCompraDto
		{
			Id = Guid.NewGuid(),
			Revision = Guid.NewGuid()
		};
		EstadoCompras estado = new EstadoCompras();
		estado.Seleccionar(pedido);
		estado.Detalle.CantidadPresentaciones = 3m;
		estado.Rechazo.Motivo = "Motivo sin enviar";
		int num = 1;
		List<PedidoCompraDto> list = new List<PedidoCompraDto>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = new PedidoCompraDto
		{
			Id = pedido.Id,
			Revision = Guid.NewGuid(),
			Estado = 1
		};
		estado.Pedidos = list;
		Assert.Same(pedido, estado.Seleccionado);
		// La selección establecida arriba sigue siendo el pedido comprobado por Same.
		Assert.Equal(pedido.Revision, estado.Seleccionado!.Revision);
		Assert.Equal(3m, estado.Detalle.CantidadPresentaciones);
		Assert.Equal("Motivo sin enviar", estado.Rechazo.Motivo);
		Assert.True(estado.TieneCambios);
		estado.Seleccionar(new PedidoCompraDto
		{
			Id = Guid.NewGuid()
		});
		Assert.Null(estado.EdicionCantidad);
		Assert.Empty(estado.Detalle.Asignaciones);
		Assert.Empty(estado.Rechazo.Motivo);
	}
}
