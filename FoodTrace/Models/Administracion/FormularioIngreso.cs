using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Administracion;

public sealed class FormularioIngreso
{
    [Required(ErrorMessage = "Ingrese su correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Ingrese su contraseña.")]
    public string Contrasena { get; set; } = "";
}
