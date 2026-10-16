using System.Text.Json.Serialization;

namespace FoodTrace.API.Dtos.Comunes;

public sealed class ErrorDto
{
    [JsonPropertyName("message")]
    public string Mensaje { get; set; } = string.Empty;
}
