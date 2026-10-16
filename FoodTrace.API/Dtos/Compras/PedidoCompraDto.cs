using FoodTrace.API.Dtos.Comunes;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Compras;

public sealed class PedidoCompraDto : EntidadEmpresaDto
{
    [JsonPropertyName("supplierId")]
    public Guid ProveedorId { get; set; }
    [JsonPropertyName("status")]
    public int Estado { get; set; }
    [JsonPropertyName("createdBy")]
    public string CreadoPorId { get; set; } = string.Empty;
    [JsonPropertyName("createdAt")]
    public DateTimeOffset FechaCreacion { get; set; }
    [JsonPropertyName("submittedBy")]
    public string? EnviadoPorId { get; set; }
    [JsonPropertyName("submittedAt")]
    public DateTimeOffset? FechaEnvio { get; set; }
    [JsonPropertyName("decidedBy")]
    public string? DecididoPorId { get; set; }
    [JsonPropertyName("decidedAt")]
    public DateTimeOffset? FechaDecision { get; set; }
    [JsonPropertyName("decisionReason")]
    public string? MotivoDecision { get; set; }
    [JsonPropertyName("lines")]
    public List<DetallePedidoCompraDto> Detalles { get; set; } = [];
}
