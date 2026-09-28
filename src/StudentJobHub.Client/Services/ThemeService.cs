using Microsoft.JSInterop;

namespace StudentJobHub.Client.Services;

public class ThemeService
{
    private const string ThemeStorageKey = "theme";
    private readonly IJSRuntime _jsRuntime;

    public bool IsDarkMode { get; private set; }
    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeThemeAsync()
    {
        var storedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeStorageKey);
        IsDarkMode = storedTheme == "dark";
        await ApplyThemeAsync();
    }

    public async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            ThemeStorageKey,
            IsDarkMode ? "dark" : "light");
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    private async Task ApplyThemeAsync()
    {
        await _jsRuntime.InvokeVoidAsync("document.body.classList.toggle", "dark-mode", IsDarkMode);
    }
}
