using System.ComponentModel.DataAnnotations;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Models.Administracion;
using FoodTrace.Models.Recetas;

namespace FoodTrace.Models.Produccion;

public sealed class ModeloPlanificacion : IValidatableObject
{
    public Guid EstablecimientoId { get; set; }
    public Guid VersionRecetaId { get; set; }
    [CantidadPositiva]
    public decimal Cantidad { get; set; } = 1;
    public string UnidadMedidaId { get; set; } = "";
    public bool PorPorciones { get; set; }
    [Required(ErrorMessage = "Seleccione la fecha de producción.")]
    public DateTime? Fecha { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EstablecimientoId == Guid.Empty) yield return new("Seleccione un establecimiento.", [nameof(EstablecimientoId)]);
        if (VersionRecetaId == Guid.Empty) yield return new("Seleccione una versión publicada.", [nameof(VersionRecetaId)]);
        if (!PorPorciones && string.IsNullOrWhiteSpace(UnidadMedidaId)) yield return new("Seleccione la unidad de la cantidad planificada.", [nameof(UnidadMedidaId)]);
        if (PorPorciones && !string.IsNullOrEmpty(UnidadMedidaId)) yield return new("No indique una unidad al planificar por porciones.", [nameof(UnidadMedidaId)]);
    }

    public List<string> Validar(CatalogosDto catalogos, List<VersionRecetaDto> versiones)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this), errores, true);
        var mensajes = errores.Select(x => x.ErrorMessage ?? "Revise la planificación.").ToList();
        if (!catalogos.Establecimientos.Any(x => x.Id == EstablecimientoId)) mensajes.Add("Seleccione un establecimiento disponible.");
        var version = versiones.FirstOrDefault(x => x.Id == VersionRecetaId && x.Publicada);
        if (version is null) mensajes.Add("Seleccione una versión publicada disponible.");
        else if (PorPorciones)
        {
            if (version.PorcionesRendimiento is not { } porciones || !ModeloVersionReceta.CantidadValida(porciones)) mensajes.Add("La versión no define una equivalencia válida de porciones.");
            else
            {
                try
                {
                    if (!ModeloVersionReceta.CantidadValida(Cantidad * version.Rendimiento / porciones)) mensajes.Add("La cantidad convertida debe ser positiva y representable con seis decimales dentro del rango admitido.");
                }
                catch (OverflowException) { mensajes.Add("La cantidad convertida supera el rango admitido."); }
            }
        }
        else
        {
            var unidad = CompatibilidadUnidades.ParaProducto(catalogos, version.ProductoResultadoId).FirstOrDefault(x => x.Id == UnidadMedidaId);
            if (unidad is null) mensajes.Add("Seleccione una unidad compatible con el producto resultado.");
            else
            {
                var producto = catalogos.Productos.First(x => x.Id == version.ProductoResultadoId);
                var unidadBase = catalogos.UnidadesMedida.First(x => x.Id == producto.UnidadBaseId);
                try
                {
                    if (!ModeloVersionReceta.CantidadValida(Cantidad * unidad.FactorConversion / unidadBase.FactorConversion))
                        mensajes.Add("La cantidad convertida debe ser positiva y representable con seis decimales dentro del rango admitido.");
                }
                catch (Exception ex) when (ex is OverflowException or DivideByZeroException)
                { mensajes.Add("La cantidad convertida debe ser positiva y representable con seis decimales dentro del rango admitido."); }
            }
        }
        return mensajes;
    }

    public PlanificarProduccionDto ParaEnviar() => new()
    {
        EstablecimientoId = EstablecimientoId, VersionRecetaId = VersionRecetaId, CantidadPlanificada = Cantidad,
        UnidadMedidaId = PorPorciones ? "" : UnidadMedidaId, PorPorciones = PorPorciones, FechaPlanificada = DateOnly.FromDateTime(Fecha!.Value)
    };

    public ModeloPlanificacion Copiar() => new()
    {
        EstablecimientoId = EstablecimientoId, VersionRecetaId = VersionRecetaId,
        Cantidad = Cantidad, UnidadMedidaId = UnidadMedidaId, PorPorciones = PorPorciones, Fecha = Fecha
    };

    public bool EquivaleA(ModeloPlanificacion modelo) => EstablecimientoId == modelo.EstablecimientoId && VersionRecetaId == modelo.VersionRecetaId
        && Cantidad == modelo.Cantidad && UnidadMedidaId == modelo.UnidadMedidaId && PorPorciones == modelo.PorPorciones && Fecha == modelo.Fecha;
}
