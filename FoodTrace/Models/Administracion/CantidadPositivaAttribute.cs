using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Administracion;

public sealed class CantidadPositivaAttribute : ValidationAttribute
{
    public CantidadPositivaAttribute() : base("Ingrese una cantidad positiva, de hasta 1.000.000.000 y con un máximo de seis decimales.") { }
    public override bool IsValid(object? value) => value is decimal cantidad && cantidad > 0 && cantidad <= 1_000_000_000m && decimal.Round(cantidad, 6) == cantidad;
}
