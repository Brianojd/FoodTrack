using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Proveedores;

public sealed class CrearPresentacionProveedorDto
{
    [JsonPropertyName("supplierId")]
    public Guid ProveedorId { get; set; }
    [JsonPropertyName("productId")]
    public Guid ProductoId { get; set; }
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;
    [JsonPropertyName("unitId")]
    public string UnidadMedidaId { get; set; } = string.Empty;
    [JsonPropertyName("quantity")]
    public decimal Cantidad { get; set; }
}
