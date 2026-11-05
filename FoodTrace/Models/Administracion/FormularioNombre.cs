using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Administracion;

public sealed class FormularioNombre
{
    [Required(ErrorMessage = "Ingrese un nombre.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string Nombre { get; set; } = "";
}
