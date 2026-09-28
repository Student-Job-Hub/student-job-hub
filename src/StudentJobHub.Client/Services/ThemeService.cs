using Microsoft.JSInterop;

namespace StudentJobHub.Client.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    public bool IsDarkMode { get; private set; }
    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeThemeAsync()
    {
        try
        {
            var storedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "theme");
            IsDarkMode = storedTheme == "dark";
            await ApplyThemeAsync();
        }
        catch
        {
            // Fallback default
            IsDarkMode = false;
        }
    }

    public async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", IsDarkMode ? "dark" : "light");
        }
        catch { }

        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    private async Task ApplyThemeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("document.body.classList.toggle", "dark-mode", IsDarkMode);
        }
        catch { }
    }
}
