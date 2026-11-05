using FoodTrace.Models.Administracion;

namespace FoodTrace.Models.Compras;

public sealed class ModeloAsignacionCompra
{
    public Guid NecesidadId { get; set; }
    public Guid Revision { get; set; }
    [CantidadPositiva] public decimal Cantidad { get; set; }
}
