using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Administracion;

public sealed class FormularioMembresia
{
    public string Id { get; set; } = "";
    public string Correo { get; set; } = "";
    public bool Habilitado { get; set; }
    [MinLength(1, ErrorMessage = "Seleccione al menos un rol.")]
    public string[] Roles { get; set; } = [];
}
