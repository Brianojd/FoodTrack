using FoodTrace.API.Dtos.Comunes;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Compras;

public sealed class AsignacionCompraDto : EntidadEmpresaDto
{
    [JsonPropertyName("purchaseLineId")]
    public Guid DetallePedidoCompraId { get; set; }
    [JsonPropertyName("requirementId")]
    public Guid NecesidadIngredienteId { get; set; }
    [JsonPropertyName("amount")]
    public decimal CantidadAsignada { get; set; }
    [JsonPropertyName("released")]
    public bool Liberada { get; set; }
}
