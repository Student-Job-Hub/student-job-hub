using System.Text.Json;
using Microsoft.JSInterop;
using StudentJobHub.Client.Models;

namespace StudentJobHub.Client.Services;

public class SettingsService
{
    private const string SettingsStorageKey = "app_user_settings";
    private readonly IJSRuntime _jsRuntime;
    private readonly ThemeService _themeService;

    public event Action? OnSettingsChanged;

    public SettingsService(IJSRuntime jsRuntime, ThemeService themeService)
    {
        _jsRuntime = jsRuntime;
        _themeService = themeService;
    }

    public async Task<UserSettingsModel> GetSettingsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SettingsStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var settings = JsonSerializer.Deserialize<UserSettingsModel>(json);
                if (settings != null)
                {
                    // Keep theme synchronized with ThemeService
                    settings.IsDarkMode = _themeService.IsDarkMode;
                    return settings;
                }
            }
        }
        catch
        {
            // Fall back to default settings
        }

        var defaultSettings = new UserSettingsModel
        {
            IsDarkMode = _themeService.IsDarkMode
        };

        return defaultSettings;
    }

    public async Task SaveSettingsAsync(UserSettingsModel settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SettingsStorageKey, json);

            // Sync theme if changed
            if (_themeService.IsDarkMode != settings.IsDarkMode)
            {
                await _themeService.ToggleThemeAsync();
            }

            OnSettingsChanged?.Invoke();
        }
        catch (Exception)
        {
            // Log or ignore client-side storage errors
        }
    }

    public async Task<UserSettingsModel> ResetToDefaultsAsync()
    {
        var defaultSettings = new UserSettingsModel();
        await SaveSettingsAsync(defaultSettings);
        return defaultSettings;
    }

    public async Task ClearAppCacheAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SettingsStorageKey);
        }
        catch
        {
            // Ignore
        }
    }
}
