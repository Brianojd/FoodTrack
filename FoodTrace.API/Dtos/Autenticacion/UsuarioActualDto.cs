using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Autenticacion;

public sealed class UsuarioActualDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("companyId")]
    public Guid EmpresaId { get; set; }
    [JsonPropertyName("email")]
    public string? Correo { get; set; }
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = [];
}
