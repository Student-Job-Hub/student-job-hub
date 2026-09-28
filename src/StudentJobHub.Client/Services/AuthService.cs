using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using StudentJobHub.Client.Models;

namespace StudentJobHub.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public string? Token { get; private set; }

    public string? RefreshToken { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public event Action? OnAuthStateChanged;

    public async Task InitializeAsync()
    {
        await GetTokenAsync();
        await GetRefreshTokenAsync();
    }

    public async Task<string?> GetTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            try
            {
                Token = await _jsRuntime.InvokeAsync<string?>("authStorage.getToken");
            }
            catch
            {
                // In case JS interop is not ready yet during pre-rendering
            }
        }

        return Token;
    }

    public async Task<string?> GetTokenForRequestAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token) || !IsTokenExpiring(token))
        {
            return token;
        }

        await _refreshLock.WaitAsync();
        try
        {
            token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token) || !IsTokenExpiring(token))
            {
                return token;
            }

            var refreshToken = await GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return token;
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(
                    "api/auth/refresh",
                    new { refreshToken });
            }
            catch (HttpRequestException)
            {
                return token;
            }

            if (!response.IsSuccessStatusCode)
            {
                await LogoutAsync();
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null || string.IsNullOrWhiteSpace(result.Token) ||
                string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                await LogoutAsync();
                return null;
            }

            await StoreTokensAsync(result);
            return Token;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginModel model)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            model);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content
            .ReadFromJsonAsync<AuthResponse>();

        if (result != null && !string.IsNullOrWhiteSpace(result.Token))
        {
            await StoreTokensAsync(result);
            OnAuthStateChanged?.Invoke();
        }

        return result;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterModel model)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            model);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content
            .ReadFromJsonAsync<AuthResponse>();

        if (result != null && !string.IsNullOrWhiteSpace(result.Token))
        {
            await StoreTokensAsync(result);
            OnAuthStateChanged?.Invoke();
        }

        return result;
    }

    public async Task LogoutAsync()
    {
        Token = null;
        RefreshToken = null;
        try
        {
            await _jsRuntime.InvokeVoidAsync("authStorage.removeToken");
        }
        catch
        {
            // Ignore if JS interop error during logout
        }
        OnAuthStateChanged?.Invoke();
    }

    private async Task<string?> GetRefreshTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(RefreshToken))
        {
            try
            {
                RefreshToken = await _jsRuntime.InvokeAsync<string?>(
                    "authStorage.getRefreshToken");
            }
            catch
            {
                // In case JS interop is not ready yet during pre-rendering
            }
        }

        return RefreshToken;
    }

    private async Task StoreTokensAsync(AuthResponse response)
    {
        Token = response.Token;
        RefreshToken = response.RefreshToken;
        await _jsRuntime.InvokeVoidAsync(
            "authStorage.setTokens",
            Token,
            RefreshToken);
    }

    private static bool IsTokenExpiring(string token)
    {
        try
        {
            var payload = token.Split('.')[1]
                .Replace('-', '+')
                .Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(
                Convert.FromBase64String(payload));
            return !document.RootElement.TryGetProperty("exp", out var expiry) ||
                expiry.GetInt64() <= DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
        }
        catch
        {
            return true;
        }
    }
}