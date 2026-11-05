using System.Text.Json.Serialization;

namespace FoodTrace.Dtos.Comunes;

public sealed class RevisionDto
{
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
    [JsonPropertyName("reason")]
    public string? Motivo { get; set; }
}
