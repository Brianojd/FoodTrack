using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Models.Compras;

namespace FoodTrace.Services.Estado;

public sealed class EstadoCompras
{
    public CatalogosDto? Catalogos { get; set; }
    public List<PedidoCompraDto> Pedidos { get; set; } = [];
    public List<NecesidadIngredienteDto> Necesidades { get; set; } = [];
    public List<OrdenProduccionDto> Produccion { get; set; } = [];
    public PedidoCompraDto? Seleccionado { get; private set; }
    public ModeloPedidoCompra Nuevo { get; set; } = new();
    public ModeloDetallePedido Detalle { get; set; } = new();
    public ModeloCantidadPresentaciones? EdicionCantidad { get; set; }
    public ModeloRechazoCompra Rechazo { get; set; } = new();
    public bool NecesidadesDesactualizadas { get; set; }
    public bool TieneCambios => Nuevo.ProveedorId != Guid.Empty || Detalle.TieneContenido || !string.IsNullOrEmpty(Rechazo.Motivo) || EdicionCantidad is { } edicion && edicion.Cantidad != edicion.CantidadOriginal;

    public void Seleccionar(PedidoCompraDto pedido)
    {
        Seleccionado = pedido; Nuevo = new(); Detalle = new(); EdicionCantidad = null; Rechazo = new();
    }
    public void AceptarRespuesta(PedidoCompraDto pedido)
    {
        Pedidos = Pedidos.Where(x => x.Id != pedido.Id).Append(pedido).ToList();
        Seleccionado = pedido;
    }
    public static string NombreEstado(int estado) => estado switch { 0 => "Borrador", 1 => "Enviado", 2 => "Aprobado · solo lectura", 3 => "Rechazado · solo lectura", _ => "Estado no reconocido · solo lectura" };
}
