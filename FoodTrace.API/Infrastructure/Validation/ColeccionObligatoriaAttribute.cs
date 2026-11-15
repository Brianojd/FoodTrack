using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace FoodTrace.API.Infrastructure.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ColeccionObligatoriaAttribute : ValidationAttribute
{
    public ColeccionObligatoriaAttribute() : base("The {0} field is required.") { }

    public override bool IsValid(object? value) => value is ICollection { Count: > 0 };
}
