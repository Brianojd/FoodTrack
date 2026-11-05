using FoodTrace.Dtos.Compras;

namespace FoodTrace.Models.Compras;

public sealed class FormularioCompras
{
    public Guid ProveedorId { get; set; }
    public Guid PresentacionProveedorId { get; set; }
    public decimal CantidadPresentaciones { get; set; } = 1;
    public decimal CantidadEdicion { get; set; } = 1;
    public string MotivoRechazo { get; set; } = "";
    public PedidoCompraDto? PedidoSeleccionado { get; set; }
    public PedidoCompraDto? PedidoEdicion { get; set; }
    public DetallePedidoCompraDto? DetalleEdicion { get; set; }
    public Dictionary<Guid, decimal> NecesidadesSeleccionadas { get; set; } = [];

    public void RestablecerSeleccion()
    {
        PedidoSeleccionado = null; PedidoEdicion = null; NecesidadesSeleccionadas.Clear();
    }

    public void Limpiar()
    {
        RestablecerSeleccion(); DetalleEdicion = null;
        ProveedorId = PresentacionProveedorId = Guid.Empty; MotivoRechazo = "";
    }
}
