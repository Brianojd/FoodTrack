using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Administracion;

public sealed class FormularioUsuario
{
    [Required(ErrorMessage = "Ingrese el correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Ingrese una contraseña inicial.")]
    public string Contrasena { get; set; } = "";
    [MinLength(1, ErrorMessage = "Seleccione al menos un rol.")]
    public string[] Roles { get; set; } = [];
}
