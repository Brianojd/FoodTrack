using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Proveedores;
using FoodTrace.Dtos.UnidadesMedida;

using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Comunes;

public sealed class CatalogosDto
{
    [JsonPropertyName("company")]
    public NombreDto Empresa { get; set; } = new();
    [JsonPropertyName("units")]
    public List<UnidadMedidaDto> UnidadesMedida { get; set; } = [];
    [JsonPropertyName("establishments")]
    public List<NombreDto> Establecimientos { get; set; } = [];
    [JsonPropertyName("products")]
    public List<ProductoDto> Productos { get; set; } = [];
    [JsonPropertyName("suppliers")]
    public List<NombreDto> Proveedores { get; set; } = [];
    [JsonPropertyName("packs")]
    public List<PresentacionProveedorDto> Presentaciones { get; set; } = [];
}
