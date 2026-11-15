using FoodTrace.API.Dtos.Empresas;
using FoodTrace.API.Dtos.Productos;
using FoodTrace.API.Dtos.Proveedores;
using FoodTrace.API.Dtos.UnidadesMedida;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Comunes;

public sealed class CatalogosDto
{
    [JsonPropertyName("company")]
    public EmpresaDto Empresa { get; set; } = new();
    [JsonPropertyName("units")]
    public List<UnidadMedidaDto> UnidadesMedida { get; set; } = [];
    [JsonPropertyName("establishments")]
    public List<EntidadConNombreDto> Establecimientos { get; set; } = [];
    [JsonPropertyName("products")]
    public List<ProductoDto> Productos { get; set; } = [];
    [JsonPropertyName("suppliers")]
    public List<EntidadConNombreDto> Proveedores { get; set; } = [];
    [JsonPropertyName("packs")]
    public List<PresentacionProveedorDto> Presentaciones { get; set; } = [];
}
