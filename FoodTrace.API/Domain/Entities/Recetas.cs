namespace FoodTrace.API.Domain.Entities;

public sealed class VersionReceta : EntidadEmpresa
{
    public Guid FamiliaRecetaId { get; set; } = Guid.NewGuid();
    public int NumeroVersion { get; set; } = 1;
    public string Nombre { get; set; } = "";
    public Guid ProductoResultadoId { get; set; }
    public decimal Rendimiento { get; set; }
    public decimal? CantidadRendimiento { get; set; }
    public string? UnidadRendimientoId { get; set; }
    public decimal? PorcionesRendimiento { get; set; }
    public string Instrucciones { get; set; } = "";
    public bool Publicada { get; set; }
    public List<IngredienteReceta> Ingredientes { get; set; } = [];
}

public sealed class IngredienteReceta : EntidadEmpresa
{
    public Guid VersionRecetaId { get; set; }
    public Guid ProductoId { get; set; }
    public string UnidadMedidaId { get; set; } = "";
    public decimal Cantidad { get; set; }
}
