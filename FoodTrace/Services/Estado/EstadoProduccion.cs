using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Models.Produccion;

namespace FoodTrace.Services.Estado;

public sealed class EstadoProduccion
{
    public CatalogosDto? Catalogos { get; set; }
    public List<VersionRecetaDto> Versiones { get; set; } = [];
    public List<OrdenProduccionDto> Ordenes { get; set; } = [];
    public List<NecesidadIngredienteDto> Necesidades { get; set; } = [];
    public ModeloPlanificacion Formulario { get; set; } = new();
    public Guid? OrdenSeleccionadaId { get; set; }
}
