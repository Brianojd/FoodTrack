using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Administracion;

public sealed class FormularioPresentacion : IValidatableObject
{
    public Guid ProveedorId { get; set; }
    public Guid ProductoId { get; set; }
    [Required(ErrorMessage = "Ingrese el nombre de la presentación.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Seleccione la unidad del contenido.")]
    public string UnidadMedidaId { get; set; } = "";
    [CantidadPositiva]
    public decimal Cantidad { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProveedorId == Guid.Empty) yield return new("Seleccione un proveedor.", [nameof(ProveedorId)]);
        if (ProductoId == Guid.Empty) yield return new("Seleccione un producto.", [nameof(ProductoId)]);
    }
}
