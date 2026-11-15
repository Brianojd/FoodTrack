using FoodTrace.Dtos.Recetas;

namespace FoodTrace.Models.Recetas;

public sealed class EdicionIngrediente
{
    public Guid ProductoId { get; set; }
    public string UnidadMedidaId { get; set; } = "g";
    public decimal Cantidad { get; set; } = 1;
}

public sealed class FormularioReceta
{
    public VersionRecetaDto? RecetaEdicion { get; set; }
    public string NombreReceta { get; set; } = "";
    public string Instrucciones { get; set; } = "";
    public Guid ProductoResultadoId { get; set; }
    public decimal Rendimiento { get; set; } = 1;
    public List<EdicionIngrediente> IngredientesBorrador { get; set; } = [];

    public void Editar(VersionRecetaDto receta)
    {
        RecetaEdicion = receta; NombreReceta = receta.Nombre; ProductoResultadoId = receta.ProductoResultadoId;
        Rendimiento = receta.Rendimiento; Instrucciones = receta.Instrucciones;
        IngredientesBorrador = receta.Ingredientes.Select(x => new EdicionIngrediente { ProductoId = x.ProductoId, UnidadMedidaId = x.UnidadMedidaId, Cantidad = x.Cantidad }).ToList();
    }

    public void Restablecer()
    {
        RecetaEdicion = null; NombreReceta = ""; ProductoResultadoId = Guid.Empty; Rendimiento = 1;
        Instrucciones = ""; IngredientesBorrador = [];
    }
}
