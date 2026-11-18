using System.Text.Json.Serialization;

namespace FoodTrace.Mobile.Models;

public sealed record LoginRequest(
    [property: JsonPropertyName("email")] string Correo,
    [property: JsonPropertyName("password")] string Contrasena);

public sealed class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
}

public sealed class CurrentUserResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("companyId")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = [];
}
