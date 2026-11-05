namespace FoodTrace.Models.Produccion;

public sealed class FormularioProduccion
{
    public Guid EstablecimientoId { get; set; }
    public Guid VersionRecetaId { get; set; }
    public decimal CantidadPlanificada { get; set; } = 1;
    public string UnidadMedidaId { get; set; } = "unit";
    public DateTime FechaPlanificada { get; set; } = DateTime.Today;
}
