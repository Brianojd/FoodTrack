using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Proveedores;

namespace FoodTrace.Models.Compras;

public sealed class ModeloPropuestaCompra
{
    public Guid EstablecimientoId { get; set; }
    public List<LineaPropuestaCompra> Lineas { get; } = [];

    public static ModeloPropuestaCompra Crear(IEnumerable<NecesidadIngredienteDto> necesidades, CatalogosDto catalogos)
    {
        var propuesta = new ModeloPropuestaCompra();
        foreach (var grupo in necesidades
            .Select(x => new { Necesidad = x, Faltante = Math.Max(x.CantidadNecesaria - x.CantidadAsignada, 0m) })
            .Where(x => x.Faltante > 0m)
            .GroupBy(x => new { x.Necesidad.ProductoId, x.Necesidad.UnidadBaseId }))
        {
            var producto = catalogos.Productos.FirstOrDefault(x => x.Id == grupo.Key.ProductoId);
            var opciones = catalogos.Presentaciones.Where(x => x.ProductoId == grupo.Key.ProductoId && x.CantidadBase > 0m).ToList();
            propuesta.Lineas.Add(new LineaPropuestaCompra
            {
                ProductoId = grupo.Key.ProductoId,
                NombreProducto = producto?.Nombre ?? grupo.Key.ProductoId.ToString(),
                UnidadBaseId = grupo.Key.UnidadBaseId,
                Faltante = grupo.Sum(x => x.Faltante),
                Necesidades = grupo.Select(x => new AsignacionPropuestaCompra(x.Necesidad.Id, x.Faltante, x.Necesidad.Revision)).ToList(),
                Opciones = opciones
            });
        }
        return propuesta;
    }
}

public sealed class LineaPropuestaCompra
{
    public Guid ProductoId { get; init; }
    public string NombreProducto { get; init; } = string.Empty;
    public string UnidadBaseId { get; init; } = string.Empty;
    public decimal Faltante { get; init; }
    public List<AsignacionPropuestaCompra> Necesidades { get; init; } = [];
    public List<PresentacionProveedorDto> Opciones { get; init; } = [];
    public Guid PresentacionId { get; set; }
    public decimal CantidadPresentaciones { get; set; }
    public bool Incluida { get; set; } = true;

    public PresentacionProveedorDto? Presentacion => Opciones.FirstOrDefault(x => x.Id == PresentacionId);
    public decimal CantidadBase => Presentacion?.CantidadBase ?? 0m;
    public decimal CantidadCubierta => CantidadPresentaciones * CantidadBase;

    public void SeleccionarPresentacion(Guid id)
    {
        PresentacionId = id;
        var presentacion = Presentacion;
        CantidadPresentaciones = presentacion is null ? 0m : decimal.Ceiling(Faltante / presentacion.CantidadBase);
    }
}

public sealed record AsignacionPropuestaCompra(Guid NecesidadId, decimal Cantidad, Guid Revision);
