using System.ComponentModel.DataAnnotations;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Models.Administracion;

namespace FoodTrace.Models.Compras;

public sealed class ModeloDetallePedido : IValidatableObject
{
    public Guid PresentacionId { get; set; }
    public Guid OrdenFiltroId { get; set; }
    [CantidadPositiva] public decimal CantidadPresentaciones { get; set; } = 1;
    public Dictionary<Guid, ModeloAsignacionCompra> Asignaciones { get; set; } = [];
    public bool TieneContenido => PresentacionId != Guid.Empty || CantidadPresentaciones != 1 || Asignaciones.Count != 0;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PresentacionId == Guid.Empty) yield return new("Seleccione una presentación del proveedor.");
        if (decimal.Truncate(CantidadPresentaciones) != CantidadPresentaciones) yield return new("La cantidad de presentaciones debe ser entera.");
        if (Asignaciones.Count > 200) yield return new("Seleccione entre cero y 200 necesidades brutas.");
        foreach (var asignacion in Asignaciones.Values)
        {
            var errores = new List<ValidationResult>();
            Validator.TryValidateObject(asignacion, new ValidationContext(asignacion), errores, true);
            foreach (var error in errores) yield return new(error.ErrorMessage);
        }
    }

    public List<string> Validar(PedidoCompraDto pedido, CatalogosDto catalogos, List<NecesidadIngredienteDto> necesidades)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this), errores, true);
        var mensajes = errores.Select(x => x.ErrorMessage ?? "Revise el detalle.").ToList();
        var presentacion = catalogos.Presentaciones.FirstOrDefault(x => x.Id == PresentacionId && x.ProveedorId == pedido.ProveedorId);
        if (presentacion is null) { mensajes.Add("Seleccione una presentación del proveedor de este pedido."); return mensajes; }
        foreach (var asignacion in Asignaciones.Values)
        {
            var necesidad = necesidades.FirstOrDefault(x => x.Id == asignacion.NecesidadId);
            if (necesidad is null || necesidad.ProductoId != presentacion.ProductoId) mensajes.Add("Todas las necesidades deben corresponder al producto de la presentación seleccionada.");
            else
            {
                var unidadBase = catalogos.Productos.FirstOrDefault(x => x.Id == presentacion.ProductoId)?.UnidadBaseId;
                if (necesidad.UnidadBaseId != unidadBase) mensajes.Add("La unidad base de la necesidad no coincide con el producto.");
                if (necesidad.Revision != asignacion.Revision) mensajes.Add("La revisión de una necesidad no coincide con la instantánea de edición. Cancele y revise los datos antes de reintentar.");
                if (asignacion.Cantidad > necesidad.CantidadNecesaria - necesidad.CantidadAsignada) mensajes.Add("Una asignación supera la necesidad bruta aún no asignada a compras. Revise los datos del servidor.");
            }
        }
        // Solo valida la cobertura ingresada; no reemplaza los importes ni revisiones oficiales.
        if (Asignaciones.Count > 0)
        {
            try
            {
                if (Asignaciones.Values.Sum(x => x.Cantidad) > CantidadPresentaciones * presentacion.CantidadBase) mensajes.Add("Las presentaciones ingresadas no cubren el total de las asignaciones seleccionadas.");
            }
            catch (OverflowException) { mensajes.Add("Las cantidades ingresadas superan el límite permitido."); }
        }
        return mensajes;
    }

    public AgregarDetalleCompraDto ParaEnviar(Guid revision) => new()
    {
        Revision = revision, PresentacionProveedorId = PresentacionId, CantidadPresentaciones = CantidadPresentaciones,
        Asignaciones = Asignaciones.Values.Select(x => new AsignacionNecesidadEntradaDto { NecesidadIngredienteId = x.NecesidadId, Revision = x.Revision, CantidadAsignada = x.Cantidad }).ToArray()
    };

    public ModeloDetallePedido Copiar() => new()
    {
        PresentacionId = PresentacionId, OrdenFiltroId = OrdenFiltroId, CantidadPresentaciones = CantidadPresentaciones,
        Asignaciones = Asignaciones.ToDictionary(x => x.Key, x => new ModeloAsignacionCompra
        {
            NecesidadId = x.Value.NecesidadId, Revision = x.Value.Revision, Cantidad = x.Value.Cantidad
        })
    };

    public bool EquivaleA(ModeloDetallePedido modelo) => PresentacionId == modelo.PresentacionId && OrdenFiltroId == modelo.OrdenFiltroId
        && CantidadPresentaciones == modelo.CantidadPresentaciones
        && Asignaciones.OrderBy(x => x.Key).Select(x => (x.Key, x.Value.NecesidadId, x.Value.Revision, x.Value.Cantidad))
            .SequenceEqual(modelo.Asignaciones.OrderBy(x => x.Key).Select(x => (x.Key, x.Value.NecesidadId, x.Value.Revision, x.Value.Cantidad)));
}
