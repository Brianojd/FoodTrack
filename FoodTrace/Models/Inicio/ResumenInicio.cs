using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Recetas;

namespace FoodTrace.Models.Inicio;

// One acknowledged snapshot of complete, company-scoped collections. No stock calculations.
public sealed class ResumenInicio
{
    public required CatalogosDto Catalogos { get; init; }
    public required List<VersionRecetaDto> Versiones { get; init; }
    public required List<OrdenProduccionDto> Ordenes { get; init; }
    public required List<PedidoCompraDto> Pedidos { get; init; }
    public DateTimeOffset Consultado { get; init; } = DateTimeOffset.Now;
    public int Familias => Versiones.Select(x => x.FamiliaRecetaId).Distinct().Count();
    public int Publicadas => Versiones.Count(x => x.Publicada);
    public int PedidosEnEstado(int estado) => Pedidos.Count(x => x.Estado == estado);
}
