using System.ComponentModel.DataAnnotations;
using FoodTrace.Models.Administracion;

namespace FoodTrace.Models.Recetas;

public sealed class ModeloIngredienteReceta : IValidatableObject
{
    public Guid Clave { get; set; } = Guid.NewGuid();
    public Guid ProductoId { get; set; }
    [Required(ErrorMessage = "Seleccione una unidad para el ingrediente.")]
    public string UnidadMedidaId { get; set; } = "";
    [CantidadPositiva]
    public decimal Cantidad { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductoId == Guid.Empty) yield return new("Seleccione el producto del ingrediente.", [nameof(ProductoId)]);
    }
}
