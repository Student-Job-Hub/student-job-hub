using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StudentJobHub.Client;
using StudentJobHub.Client.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();

var apiBaseUrl = "http://localhost:5205/";

// Authentication service
builder.Services.AddScoped<AuthService>(sp =>
    new AuthService(
        new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        },
        sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));

// JWT handler
builder.Services.AddScoped<JwtAuthorizationHandler>();

// Authorized API client
builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<JwtAuthorizationHandler>();

// API services
builder.Services.AddScoped<JobApiService>(sp =>
    new JobApiService(
        sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AuthorizedClient")));

builder.Services.AddScoped<ApplicationApiService>(sp =>
    new ApplicationApiService(
        sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AuthorizedClient")));

builder.Services.AddScoped<ServiceApiService>(sp =>
    new ServiceApiService(
        sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AuthorizedClient")));

builder.Services.AddScoped<ReviewApiService>(sp =>
    new ReviewApiService(
        sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AuthorizedClient")));

builder.Services.AddScoped<NotificationService>(sp =>
    new NotificationService(
        sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AuthorizedClient")));

builder.Services.AddScoped<UserApiService>(sp =>
    new UserApiService(
        sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AuthorizedClient")));

// UI Polish services
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ThemeService>();

var host = builder.Build();

// Restore JWT from localStorage
var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

// Initialize theme preference
var themeService = host.Services.GetRequiredService<ThemeService>();
await themeService.InitializeThemeAsync();

await host.RunAsync();