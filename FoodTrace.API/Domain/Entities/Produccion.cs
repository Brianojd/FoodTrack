namespace FoodTrace.API.Domain.Entities;

public sealed class OrdenProduccion : EntidadEmpresa
{
    public Guid EstablecimientoId { get; set; }
    public Guid VersionRecetaId { get; set; }
    public decimal CantidadPlanificada { get; set; }
    public DateOnly FechaPlanificada { get; set; }
    public string CreadoPorId { get; set; } = "";
    public DateTimeOffset FechaCreacion { get; set; }
    public List<NecesidadIngrediente> Necesidades { get; set; } = [];
}

public sealed class NecesidadIngrediente : EntidadEmpresa
{
    public Guid OrdenProduccionId { get; set; }
    public Guid ProductoId { get; set; }
    public string UnidadBaseId { get; set; } = "";
    public decimal CantidadNecesaria { get; set; }
    public decimal CantidadAsignada { get; set; }
}
