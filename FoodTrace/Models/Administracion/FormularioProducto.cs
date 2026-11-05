using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Administracion;

public sealed class FormularioProducto
{
    [Required(ErrorMessage = "Ingrese el nombre del producto.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Seleccione la unidad base.")]
    public string UnidadBaseId { get; set; } = "";
}
