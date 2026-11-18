using FoodTrace.Mobile.Models;

namespace FoodTrace.Mobile.Services;

public sealed class SessionService(ApiClient apiClient)
{
    private const string AccessTokenKey = "foodtrace.access-token";

    public ApiClient ApiClient => apiClient;
    public CurrentUserResponse? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public async Task<CurrentUserResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await apiClient.PostAsync<LoginRequest, LoginResponse>(
            "auth/login", new LoginRequest(email, password), cancellationToken);
        if (string.IsNullOrWhiteSpace(response.AccessToken))
            throw new ApiClientException("La API no devolvió una sesión válida.", null, null);

        apiClient.SetAccessToken(response.AccessToken);
        try
        {
            var user = await apiClient.GetAsync<CurrentUserResponse>("api/me", cancellationToken);
            await SecureStorage.Default.SetAsync(AccessTokenKey, response.AccessToken);
            CurrentUser = user;
            return user;
        }
        catch
        {
            await LogoutAsync();
            throw;
        }
    }

    public async Task<bool> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var token = await SecureStorage.Default.GetAsync(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token)) return false;

        apiClient.SetAccessToken(token);
        try
        {
            CurrentUser = await apiClient.GetAsync<CurrentUserResponse>("api/me", cancellationToken);
            return true;
        }
        catch
        {
            await LogoutAsync();
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        apiClient.SetAccessToken(null);
        CurrentUser = null;
        SecureStorage.Default.Remove(AccessTokenKey);
        await Task.CompletedTask;
    }
}
