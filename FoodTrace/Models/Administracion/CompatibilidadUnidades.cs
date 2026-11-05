using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.UnidadesMedida;

namespace FoodTrace.Models.Administracion;

public static class CompatibilidadUnidades
{
    // Esta comprobación orienta el formulario; no sustituye la validación ni la conversión del servidor.
    public static IEnumerable<UnidadMedidaDto> ParaProducto(CatalogosDto catalogos, Guid productoId)
    {
        var producto = catalogos.Productos.FirstOrDefault(x => x.Id == productoId);
        var unidadBase = catalogos.UnidadesMedida.FirstOrDefault(x => x.Id == producto?.UnidadBaseId);
        return unidadBase is null ? [] : catalogos.UnidadesMedida.Where(x => x.Dimension == unidadBase.Dimension);
    }
}
