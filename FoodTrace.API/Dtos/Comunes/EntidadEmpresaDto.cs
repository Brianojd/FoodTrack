using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Comunes;

public abstract class EntidadEmpresaDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("companyId")]
    public Guid EmpresaId { get; set; }
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
}
