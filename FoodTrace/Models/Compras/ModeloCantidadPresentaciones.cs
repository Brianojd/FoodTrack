using System.ComponentModel.DataAnnotations;
using FoodTrace.Models.Administracion;
using FoodTrace.Dtos.Compras;

namespace FoodTrace.Models.Compras;

public sealed class ModeloCantidadPresentaciones : IValidatableObject
{
    public Guid DetalleId { get; set; }
    public decimal CantidadOriginal { get; set; }
    [CantidadPositiva] public decimal Cantidad { get; set; } = 1;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Truncate(Cantidad) != Cantidad) yield return new("La cantidad de presentaciones debe ser un número entero.", [nameof(Cantidad)]);
    }

    public List<string> Validar(DetallePedidoCompraDto detalle)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this), errores, true);
        var mensajes = errores.Select(x => x.ErrorMessage ?? "Revise la cantidad.").ToList();
        if (DetalleId != detalle.Id) mensajes.Add("El detalle no corresponde a esta edición.");
        try
        {
            if (Cantidad * detalle.CantidadBasePresentacion < detalle.NecesidadBruta)
                mensajes.Add("La cantidad de presentaciones debe cubrir la necesidad bruta guardada. La API determina la cantidad oficial.");
        }
        catch (OverflowException) { mensajes.Add("La cantidad ingresada supera el límite permitido."); }
        return mensajes;
    }
}
