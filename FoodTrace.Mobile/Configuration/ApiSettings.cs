namespace FoodTrace.Mobile.Configuration;

public sealed record ApiSettings(string EnvironmentName, string ApiBaseUrl, int TimeoutSeconds)
{
    public static ApiSettings Current { get; } = new(
        EnvironmentName: "Development",
#if ANDROID
        ApiBaseUrl: "http://192.168.0.208:6088/",
#else
        ApiBaseUrl: "http://localhost:6088/",
#endif
        TimeoutSeconds: 30);

    public bool IsValid(out string message)
    {
        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uri) || uri is null || uri.Scheme is not ("http" or "https"))
        {
            message = "La URL de la API no es válida. Revise Configuration/ApiSettings.cs.";
            return false;
        }

        if (TimeoutSeconds <= 0)
        {
            message = "El timeout de la API debe ser mayor que cero. Revise Configuration/ApiSettings.cs.";
            return false;
        }

        if (uri.AbsolutePath.Length == 0) { message = "La URL de la API debe terminar en una ruta válida."; return false; }
        message = string.Empty;
        return true;
    }

    public Uri? BaseAddress => Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uri) ? uri : null;
}
