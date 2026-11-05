using FoodTrace.API.Infrastructure.Validation;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Usuarios;

public sealed class ActualizarMembresiaDto
{
    [JsonPropertyName("enabled")]
    public bool Habilitado { get; set; }
    [JsonPropertyName("roles")]
    [ColeccionObligatoria] public string[] Roles { get; set; } = [];
}
