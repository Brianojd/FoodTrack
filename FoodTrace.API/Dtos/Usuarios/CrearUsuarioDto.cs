using System.ComponentModel.DataAnnotations;
using FoodTrace.API.Infrastructure.Validation;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Usuarios;

public sealed class CrearUsuarioDto
{
    [JsonPropertyName("email")]
    [Required] public string Correo { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    [Required] public string Contrasena { get; set; } = string.Empty;
    [JsonPropertyName("roles")]
    [ColeccionObligatoria] public string[] Roles { get; set; } = [];
}
