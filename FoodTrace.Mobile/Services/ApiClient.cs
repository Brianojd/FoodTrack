using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodTrace.Mobile.Configuration;
using Microsoft.Extensions.Logging;
namespace FoodTrace.Mobile.Services;

public sealed class ApiClient(HttpClient httpClient, ApiSettings settings, ILogger<ApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string? accessToken;

    public void SetAccessToken(string? token) => accessToken = string.IsNullOrWhiteSpace(token) ? null : token;

    public async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthorization(request);
        using var response = await SendAsync(() => httpClient.SendAsync(request, cancellationToken), cancellationToken);
        return await DeserializeAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        AddAuthorization(message);
        using var response = await SendAsync(() => httpClient.SendAsync(message, cancellationToken), cancellationToken);
        return await DeserializeAsync<TResponse>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        EnsureConfiguration();
        try
        {
            return await send();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiClientException("La solicitud a la API superó el tiempo de espera.", null, null);
        }
        catch (HttpRequestException ex)
        {
#if DEBUG
            logger.LogError(
                ex,
                "Fallo de conexión con la API. Detalle: {Detalle}",
                ex.ToString());
#endif

            throw new ApiClientException(
                "No se pudo conectar con la API. Revise la red y la URL configurada.",
                null,
                ex);
        }
    }

    private static async Task<TResponse> DeserializeAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw await CrearErrorAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken)
            ?? throw new ApiClientException("La API devolvió una respuesta vacía inesperada.", response.StatusCode, null);
    }

    private static async Task<ApiClientException> CrearErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var cuerpo = await response.Content.ReadAsStringAsync(cancellationToken);
        var esLogin = response.RequestMessage?.RequestUri?.AbsolutePath.EndsWith("auth/login", StringComparison.OrdinalIgnoreCase) == true;
        var mensaje = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized when esLogin => "El correo o la contraseña no son válidos.",
            HttpStatusCode.Unauthorized => "La sesión no es válida o expiró. Iniciá sesión nuevamente.",
            HttpStatusCode.Forbidden => "La cuenta no tiene permiso para realizar esta operación.",
            HttpStatusCode.NotFound => "No se encontró el registro solicitado en tu empresa.",
            HttpStatusCode.Conflict => "El registro cambió o ya fue procesado. Actualizá e intentá nuevamente.",
            HttpStatusCode.BadRequest => "La API rechazó los datos enviados.",
            _ when (int)response.StatusCode >= 500 => "La API informó un error del servidor. Inténtelo nuevamente más tarde.",
            _ => "La API no pudo completar la operación. Inténtelo nuevamente."
        };
        try
        {
            var problema = JsonSerializer.Deserialize<ProblemDetailsResponse>(cuerpo, JsonOptions);
            if (problema is not null && EsProblemDetailsValido(problema))
            {
                var validaciones = problema.Errors?
                    .Where(error => error.Value is { Length: > 0 })
                    .Select(error => $"{string.Join(" ", error.Value!)}")
                    .ToArray() ?? [];
                mensaje = validaciones.Length > 0
                    ? string.Join(" ", validaciones)
                    : !string.IsNullOrWhiteSpace(problema.Detail) ? problema.Detail! : problema.Title ?? mensaje;
            }
        }
        catch (JsonException) { }

        return new ApiClientException(mensaje, response.StatusCode, null);
    }

    private static bool EsProblemDetailsValido(ProblemDetailsResponse problema) =>
        !string.IsNullOrWhiteSpace(problema.Title) || !string.IsNullOrWhiteSpace(problema.Detail) || problema.Status is not null || problema.Errors is not null;

    private void AddAuthorization(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private void EnsureConfiguration()
    {
        if (!settings.IsValid(out var message) || settings.BaseAddress is null)
            throw new ApiClientException(message, null, null);
    }
}

internal sealed class ProblemDetailsResponse
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public sealed class ApiClientException(string message, HttpStatusCode? statusCode, Exception? innerException, string? responseBody = null)
    : Exception(message, innerException)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
}
