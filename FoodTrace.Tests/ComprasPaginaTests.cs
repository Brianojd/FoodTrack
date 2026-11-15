using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FoodTrace.Components.Layout;
using FoodTrace.Components.Pages.Compras;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Models.Compras;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests;

public sealed class ComprasPaginaTests
{
	private sealed class PaginaPrueba : Compras
	{
		public bool Confirmar { get; set; }

		public string Mensaje => Error;

		public PaginaPrueba(ClienteFoodTraceApi cliente, EstadoAdministracion estado)
		{
			Sesion = cliente;
			Catalogos = cliente;
			ComprasApi = cliente;
			ProduccionApi = cliente;
			Formularios = estado;
			Registro = NullLogger<PaginaAdministrativa>.Instance;
		}

		public Task InicializarAsync()
		{
			return OnInitializedAsync();
		}

		public Task AgregarPruebaAsync()
		{
			return AgregarAsync();
		}

		public Task ActualizarPruebaAsync()
		{
			return ActualizarAsync();
		}

		public Task SeleccionarPruebaAsync(PedidoCompraDto pedido)
		{
			return SeleccionarAsync(pedido);
		}

		public Task RetirarPruebaAsync(DetallePedidoCompraDto detalle)
		{
			return RetirarAsync(detalle);
		}

		protected override Task<bool> ConfirmarAsync(string titulo, string mensaje)
		{
			return Task.FromResult(Confirmar);
		}
	}

	[Fact]
	public async Task Un_409_real_conserva_todos_los_campos_ids_y_revisiones_sin_reintento()
	{
		TransporteCompras transporte = new TransporteCompras
		{
			Conflicto = true
		};
		PedidoCompraDto pedido = transporte.PrepararPedido();
		using ClienteFoodTraceApi cliente = CrearCliente(transporte);
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		using PaginaPrueba pagina = new PaginaPrueba(cliente, estado);
		await pagina.InicializarAsync();
		PrepararDetalle(estado, transporte);
		ModeloDetallePedido formulario = estado.Compras.Detalle;
		Guid revision = pedido.Revision;
		await pagina.AgregarPruebaAsync();
		Assert.Equal(1, transporte.EnviosDetalle);
		Assert.Same(formulario, estado.Compras.Detalle);
		// PrepararDetalle selecciona el único pedido; la operación conserva esa selección.
		Assert.Equal(pedido.Id, estado.Compras.Seleccionado!.Id);
		Assert.Equal(revision, estado.Compras.Seleccionado!.Revision);
		Assert.Equal(transporte.PresentacionId, formulario.PresentacionId);
		Assert.Equal(2m, formulario.CantidadPresentaciones);
		Assert.Equal(transporte.OrdenId, formulario.OrdenFiltroId);
		Assert.Equal(transporte.RevisionNecesidad, Assert.Single(formulario.Asignaciones).Value.Revision);
		Assert.Equal(10m, Assert.Single(formulario.Asignaciones).Value.Cantidad);
		// El transporte registra la entrada antes de devolver el conflicto HTTP.
		Assert.Equal(revision, transporte.Entrada!.Revision);
		Assert.Equal(transporte.RevisionNecesidad, Assert.Single(transporte.Entrada!.Asignaciones).Revision);
		Assert.Contains("conserva", pagina.Mensaje);
		Assert.DoesNotContain("INTERNAL", pagina.Mensaje);
	}

	[Fact]
	public async Task Una_escritura_pendiente_no_se_duplica_ni_admite_cambiar_de_pedido_y_se_cancela_al_disponer()
	{
		TransporteCompras transporte = new TransporteCompras
		{
			Pendiente = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously)
		};
		PedidoCompraDto pedido = transporte.PrepararPedido();
		using ClienteFoodTraceApi cliente = CrearCliente(transporte);
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		PaginaPrueba pagina = new PaginaPrueba(cliente, estado);
		await pagina.InicializarAsync();
		PrepararDetalle(estado, transporte);
		Task primera = pagina.AgregarPruebaAsync();
		await pagina.AgregarPruebaAsync();
		await pagina.SeleccionarPruebaAsync(new PedidoCompraDto
		{
			Id = Guid.NewGuid()
		});
		Assert.Equal(1, transporte.EnviosDetalle);
		// La escritura pendiente conserva la selección establecida por PrepararDetalle.
		Assert.Equal(pedido.Id, estado.Compras.Seleccionado!.Id);
		pagina.Dispose();
		await primera;
		Assert.True(transporte.Cancelada);
		Assert.Single(estado.Compras.Detalle.Asignaciones);
		Assert.Empty(pagina.Mensaje);
	}

	[Fact]
	public async Task Seleccionar_otro_pedido_exige_decision_y_nunca_arrastra_un_id_de_detalle_anterior()
	{
		TransporteCompras transporte = new TransporteCompras();
		PedidoCompraDto pedido = transporte.PrepararPedido();
		using ClienteFoodTraceApi cliente = CrearCliente(transporte);
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		using PaginaPrueba pagina = new PaginaPrueba(cliente, estado);
		await pagina.InicializarAsync();
		PrepararDetalle(estado, transporte);
		estado.Compras.EdicionCantidad = new ModeloCantidadPresentaciones
		{
			DetalleId = pedido.Detalles[0].Id,
			CantidadOriginal = 1m,
			Cantidad = 2m
		};
		PedidoCompraDto siguiente = new PedidoCompraDto
		{
			Id = Guid.NewGuid(),
			Revision = Guid.NewGuid()
		};
		pagina.Confirmar = false;
		await pagina.SeleccionarPruebaAsync(siguiente);
		// Cancelar el diálogo conserva el pedido que PrepararDetalle seleccionó.
		Assert.Equal(pedido.Id, estado.Compras.Seleccionado!.Id);
		Assert.NotNull(estado.Compras.EdicionCantidad);
		pagina.Confirmar = true;
		await pagina.SeleccionarPruebaAsync(siguiente);
		Assert.Equal(siguiente.Id, estado.Compras.Seleccionado!.Id);
		Assert.Null(estado.Compras.EdicionCantidad);
		Assert.Empty(estado.Compras.Detalle.Asignaciones);
		await pagina.RetirarPruebaAsync(pedido.Detalles[0]);
		Assert.Equal(0, transporte.Retiros);
	}

	[Fact]
	public async Task Una_actualizacion_fallida_no_publica_cache_parcial_ni_borra_formularios()
	{
		TransporteCompras transporte = new TransporteCompras();
		transporte.PrepararPedido();
		using ClienteFoodTraceApi cliente = CrearCliente(transporte);
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		using PaginaPrueba pagina = new PaginaPrueba(cliente, estado);
		await pagina.InicializarAsync();
		PrepararDetalle(estado, transporte);
		CatalogosDto? catalogos = estado.Compras.Catalogos;
		List<PedidoCompraDto> pedidos = estado.Compras.Pedidos;
		List<NecesidadIngredienteDto> necesidades = estado.Compras.Necesidades;
		List<OrdenProduccionDto> produccion = estado.Compras.Produccion;
		transporte.FallarNecesidades = true;
		await pagina.ActualizarPruebaAsync();
		Assert.Same(catalogos, estado.Compras.Catalogos);
		Assert.Same(pedidos, estado.Compras.Pedidos);
		Assert.Same(necesidades, estado.Compras.Necesidades);
		Assert.Same(produccion, estado.Compras.Produccion);
		Assert.Single(estado.Compras.Detalle.Asignaciones);
		Assert.NotEmpty(pagina.Mensaje);
	}

	[Fact]
	public async Task Una_escritura_exitosa_adopta_solo_la_revision_devuelta_y_actualiza_necesidades_sin_duplicar()
	{
		TransporteCompras transporte = new TransporteCompras
		{
			Pendiente = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously)
		};
		PedidoCompraDto pedido = transporte.PrepararPedido();
		using ClienteFoodTraceApi cliente = CrearCliente(transporte);
		await cliente.IniciarSesionAsync(new IniciarSesionDto());
		using EstadoAdministracion estado = new EstadoAdministracion(cliente);
		using PaginaPrueba pagina = new PaginaPrueba(cliente, estado);
		await pagina.InicializarAsync();
		PrepararDetalle(estado, transporte);
		Task operacion = pagina.AgregarPruebaAsync();
		await pagina.AgregarPruebaAsync();
		Guid revision = Guid.NewGuid();
		transporte.Pendiente.SetResult(TransporteCompras.Respuesta(new PedidoCompraDto
		{
			Id = pedido.Id,
			ProveedorId = pedido.ProveedorId,
			Revision = revision,
			Detalles = pedido.Detalles
		}));
		await operacion;
		Assert.Equal(1, transporte.EnviosDetalle);
		// La respuesta guardada actualiza el pedido seleccionado durante la inicialización.
		Assert.Equal(revision, estado.Compras.Seleccionado!.Revision);
		Assert.Empty(estado.Compras.Detalle.Asignaciones);
		Assert.False(estado.Compras.NecesidadesDesactualizadas);
		Assert.Equal(transporte.RevisionNecesidad, Assert.Single(estado.Compras.Necesidades).Revision);
	}

	private static ClienteFoodTraceApi CrearCliente(TransporteCompras transporte)
	{
		return new ClienteFoodTraceApi(new HttpClient(transporte)
		{
			BaseAddress = new Uri("https://example.invalid")
		});
	}

	private static void PrepararDetalle(EstadoAdministracion estado, TransporteCompras transporte)
	{
		estado.Compras.Seleccionar(estado.Compras.Pedidos.Single());
		estado.Compras.Detalle.PresentacionId = transporte.PresentacionId;
		estado.Compras.Detalle.CantidadPresentaciones = 2m;
		estado.Compras.Detalle.OrdenFiltroId = transporte.OrdenId;
		estado.Compras.Detalle.Asignaciones[transporte.NecesidadId] = new ModeloAsignacionCompra
		{
			NecesidadId = transporte.NecesidadId,
			Revision = transporte.RevisionNecesidad,
			Cantidad = 10m
		};
	}
}
