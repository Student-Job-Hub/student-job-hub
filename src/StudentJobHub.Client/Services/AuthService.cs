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

    public UserModel? User
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                return null;
            }

            try
            {
                var payload = Token.Split('.')[1]
                    .Replace('-', '+')
                    .Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
                var user = new UserModel();

                foreach (var claim in document.RootElement.EnumerateObject())
                {
                    var name = claim.Name;
                    if (name.Equals("sub", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("nameid", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase))
                    {
                        user.Id = claim.Value.GetString() ?? string.Empty;
                    }
                    else if (name.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                             name.EndsWith("/name", StringComparison.OrdinalIgnoreCase))
                    {
                        user.FullName = claim.Value.GetString() ?? string.Empty;
                    }
                    else if (name.Equals("email", StringComparison.OrdinalIgnoreCase) ||
                             name.EndsWith("/emailaddress", StringComparison.OrdinalIgnoreCase))
                    {
                        user.Email = claim.Value.GetString() ?? string.Empty;
                    }
                    else if (name.Equals("mobilephone", StringComparison.OrdinalIgnoreCase) ||
                             name.EndsWith("/mobilephone", StringComparison.OrdinalIgnoreCase))
                    {
                        user.PhoneNumber = claim.Value.GetString();
                    }
                    else if (name.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                             name.Equals("roles", StringComparison.OrdinalIgnoreCase) ||
                             name.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
                    {
                        if (claim.Value.ValueKind == JsonValueKind.String)
                        {
                            var roleStr = claim.Value.GetString();
                            if (!string.IsNullOrEmpty(roleStr)) user.Roles.Add(roleStr);
                        }
                        else if (claim.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in claim.Value.EnumerateArray())
                            {
                                var roleStr = item.GetString();
                                if (!string.IsNullOrEmpty(roleStr)) user.Roles.Add(roleStr);
                            }
                        }
                    }
                }

                return user;
            }
            catch
            {
                return null;
            }
        }
    }

    public bool HasRole(string role)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return false;
        }

        try
        {
            var payload = Token.Split('.')[1]
                .Replace('-', '+')
                .Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            foreach (var claim in document.RootElement.EnumerateObject())
            {
                if (claim.Name != "role" && claim.Name != "roles" &&
                    !claim.Name.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (claim.Value.ValueKind == JsonValueKind.String &&
                    string.Equals(claim.Value.GetString(), role, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (claim.Value.ValueKind == JsonValueKind.Array &&
                    claim.Value.EnumerateArray().Any(value =>
                        string.Equals(value.GetString(), role, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

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