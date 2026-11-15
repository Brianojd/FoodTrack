using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Proveedores;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests.Support;

internal sealed class TransporteCompras(string rol = "buyer", int estado = 0) : HttpMessageHandler
{
	public Guid ProveedorId { get; } = Guid.NewGuid();

	public Guid ProductoId { get; } = Guid.NewGuid();

	public Guid PresentacionId { get; } = Guid.NewGuid();

	public Guid NecesidadId { get; } = Guid.NewGuid();

	public Guid RevisionNecesidad { get; } = Guid.NewGuid();

	public Guid OrdenId { get; } = Guid.NewGuid();

	public PedidoCompraDto Pedido { get; } = new PedidoCompraDto
	{
		Id = Guid.NewGuid(),
		Revision = Guid.NewGuid()
	};

	public int EnviosDetalle { get; private set; }

	public int Retiros { get; private set; }

	public bool Conflicto { get; set; }

	public bool FallarNecesidades { get; set; }

	public bool Cancelada { get; private set; }

	public AgregarDetalleCompraDto? Entrada { get; private set; }

	public TaskCompletionSource<HttpResponseMessage>? Pendiente { get; set; }

	public PedidoCompraDto PrepararPedido()
	{
		Pedido.ProveedorId = ProveedorId;
		Pedido.Estado = estado;
		Pedido.CreadoPorId = "creador-sintetico";
		Pedido.FechaCreacion = new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
		Pedido.EnviadoPorId = "comprador-sintetico";
		Pedido.FechaEnvio = Pedido.FechaCreacion.AddHours(1.0);
		Pedido.DecididoPorId = "aprobador-sintetico";
		Pedido.FechaDecision = Pedido.FechaCreacion.AddHours(2.0);
		Pedido.MotivoDecision = "Motivo persistido de prueba";
		PedidoCompraDto pedido = Pedido;
		int num = 1;
		List<DetallePedidoCompraDto> list = new List<DetallePedidoCompraDto>(num);
		CollectionsMarshal.SetCount(list, num);
		ref DetallePedidoCompraDto reference = ref CollectionsMarshal.AsSpan(list)[0];
		DetallePedidoCompraDto detallePedidoCompraDto = new DetallePedidoCompraDto
		{
			Id = Guid.NewGuid(),
			ProductoId = ProductoId,
			NombrePresentacion = "Nombre histórico guardado",
			CantidadBasePresentacion = 10m,
			CantidadPresentaciones = 1m,
			CantidadPedida = 10m,
			NecesidadBruta = 10m,
			UnidadBaseId = "kg"
		};
		int num2 = 1;
		List<AsignacionCompraDto> list2 = new List<AsignacionCompraDto>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		CollectionsMarshal.AsSpan(list2)[0] = new AsignacionCompraDto
		{
			NecesidadIngredienteId = NecesidadId,
			CantidadAsignada = 10m
		};
		detallePedidoCompraDto.Asignaciones = list2;
		reference = detallePedidoCompraDto;
		pedido.Detalles = list;
		return Pedido;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		// HttpClient resuelve la URI; el endpoint de alta recibe contenido JSON del cliente tipado.
		string ruta = request.RequestUri!.AbsolutePath;
		if (ruta.EndsWith("/lines", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
		{
			EnviosDetalle++;
			Entrada = await request.Content!.ReadFromJsonAsync<AgregarDetalleCompraDto>(cancellationToken);
			if (Pendiente != null)
			{
				try
				{
					return await Pendiente.Task.WaitAsync(cancellationToken);
				}
				catch (OperationCanceledException)
				{
					Cancelada = true;
					throw;
				}
			}
			if (Conflicto)
			{
				return new HttpResponseMessage(HttpStatusCode.Conflict)
				{
					Content = JsonContent.Create(new
					{
						message = "INTERNAL confidential stack"
					})
				};
			}
			return Respuesta(Pedido);
		}
		if (ruta.EndsWith("/remove", StringComparison.Ordinal))
		{
			Retiros++;
			return Respuesta(Pedido);
		}
		if (ruta == "/api/needs" && FallarNecesidades)
		{
			return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
		}
		if (1 == 0)
		{
		}
		object obj;
		switch (ruta)
		{
		case "/auth/login":
			obj = new
			{
				accessToken = "token-sintetico"
			};
			break;
		case "/api/me":
		{
			UsuarioActualDto usuarioActualDto = new UsuarioActualDto();
			usuarioActualDto.Id = "cuenta-sintetica";
			usuarioActualDto.Roles = new string[1] { rol };
			obj = usuarioActualDto;
			break;
		}
		case "/api/purchases":
			obj = new List<PedidoCompraDto> { Pedido };
			break;
		case "/api/needs":
			obj = new List<NecesidadIngredienteDto>
			{
				new NecesidadIngredienteDto
				{
					Id = NecesidadId,
					Revision = RevisionNecesidad,
					OrdenProduccionId = OrdenId,
					ProductoId = ProductoId,
					UnidadBaseId = "kg",
					CantidadNecesaria = 20m
				}
			};
			break;
		case "/api/production":
			obj = new List<OrdenProduccionDto>
			{
				new OrdenProduccionDto
				{
					Id = OrdenId,
					FechaPlanificada = new DateOnly(2026, 10, 15)
				}
			};
			break;
		default:
			obj = new CatalogosDto
			{
				Proveedores = new List<NombreDto>(1)
				{
					new NombreDto
					{
						Id = ProveedorId,
						Nombre = "Proveedor de prueba"
					}
				},
				Productos = new List<ProductoDto>(1)
				{
					new ProductoDto
					{
						Id = ProductoId,
						Nombre = "Carne",
						UnidadBaseId = "kg"
					}
				},
				Presentaciones = new List<PresentacionProveedorDto>(1)
				{
					new PresentacionProveedorDto
					{
						Id = PresentacionId,
						ProveedorId = ProveedorId,
						ProductoId = ProductoId,
						Nombre = "Nombre actual distinto",
						CantidadBase = 50m
					}
				}
			};
			break;
		}
		if (1 == 0)
		{
		}
		object cuerpo = obj;
		return Respuesta(cuerpo);
	}

	public static HttpResponseMessage Respuesta(object cuerpo)
	{
		return new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = JsonContent.Create(cuerpo)
		};
	}
}
