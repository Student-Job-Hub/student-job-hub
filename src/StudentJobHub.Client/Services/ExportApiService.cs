using System.Net.Http.Json;
using Microsoft.JSInterop;
using StudentJobHub.Client.Models;

namespace StudentJobHub.Client.Services;

public class ExportApiService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    public ExportApiService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> DownloadApplicationsCsvAsync()
    {
        return await DownloadFileAsync("api/Export/applications/csv", $"applications_history_{DateTime.UtcNow:yyyyMMdd}.csv", "text/csv");
    }

    public async Task<bool> DownloadJobsCsvAsync()
    {
        return await DownloadFileAsync("api/Export/jobs/csv", $"posted_jobs_{DateTime.UtcNow:yyyyMMdd}.csv", "text/csv");
    }

    public async Task<bool> DownloadBookingsCsvAsync()
    {
        return await DownloadFileAsync("api/Export/bookings/csv", $"service_bookings_{DateTime.UtcNow:yyyyMMdd}.csv", "text/csv");
    }

    public async Task<ExportSummaryModel?> GetSummaryJsonAsync()
    {
        return await _httpClient.GetFromJsonAsync<ExportSummaryModel>("api/Export/summary");
    }

    public async Task<string> GetPrintableReportHtmlAsync()
    {
        var response = await _httpClient.GetAsync("api/Export/report/html");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task OpenPrintReportAsync()
    {
        var html = await GetPrintableReportHtmlAsync();
        await _jsRuntime.InvokeVoidAsync("studentHubExport.openPrintWindow", html);
    }

    private async Task<bool> DownloadFileAsync(string requestUrl, string filename, string contentType)
    {
        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode) return false;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var base64 = Convert.ToBase64String(bytes);

            await _jsRuntime.InvokeVoidAsync("studentHubExport.downloadFromBase64", filename, contentType, base64);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
