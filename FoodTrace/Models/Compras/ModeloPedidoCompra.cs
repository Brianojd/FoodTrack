using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Compras;

public sealed class ModeloPedidoCompra : IValidatableObject
{
    public Guid ProveedorId { get; set; }
    public Guid EstablecimientoId { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProveedorId == Guid.Empty) yield return new("Seleccione un proveedor.", [nameof(ProveedorId)]);
        if (EstablecimientoId == Guid.Empty) yield return new("Seleccione un establecimiento destino.", [nameof(EstablecimientoId)]);
    }
}
