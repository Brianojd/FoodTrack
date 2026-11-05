using System.ComponentModel.DataAnnotations;

namespace FoodTrace.Models.Compras;

public sealed class ModeloRechazoCompra
{
    [Required(ErrorMessage = "Ingrese el motivo del rechazo.")]
    [StringLength(500, ErrorMessage = "El motivo admite hasta 500 caracteres.")]
    public string Motivo { get; set; } = "";
}
