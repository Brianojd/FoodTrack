using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Autenticacion;

public sealed class IniciarSesionDto
{
    [JsonPropertyName("email")]
    [Required] public string Correo { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    [Required] public string Contrasena { get; set; } = string.Empty;
}
