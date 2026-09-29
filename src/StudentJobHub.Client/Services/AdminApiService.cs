using System.Net.Http.Json;
using StudentJobHub.Client.Models;

namespace StudentJobHub.Client.Services;

public class AdminApiService
{
    private readonly HttpClient _httpClient;

    public AdminApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AdminOverviewModel?> GetOverviewAsync()
    {
        return await _httpClient.GetFromJsonAsync<AdminOverviewModel>("api/admin/overview");
    }

    public async Task<List<AdminUserModel>> GetUsersAsync(string? search = null)
    {
        var url = "api/admin/users";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"?search={Uri.EscapeDataString(search.Trim())}";
        }

        return await _httpClient.GetFromJsonAsync<List<AdminUserModel>>(url)
            ?? new List<AdminUserModel>();
    }

    public Task<HttpResponseMessage> SetUserSuspensionAsync(string userId, bool isSuspended)
    {
        return _httpClient.PatchAsJsonAsync(
            $"api/admin/users/{Uri.EscapeDataString(userId)}/suspension",
            new { isSuspended });
    }

    public async Task<List<AdminJobModel>> GetJobsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<AdminJobModel>>("api/admin/jobs")
            ?? new List<AdminJobModel>();
    }

    public Task<HttpResponseMessage> CloseJobAsync(int jobId)
    {
        return _httpClient.PatchAsync($"api/admin/jobs/{jobId}/close", null);
    }

    public async Task<List<AdminServiceModel>> GetServicesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<AdminServiceModel>>("api/admin/services")
            ?? new List<AdminServiceModel>();
    }

    public Task<HttpResponseMessage> DeleteServiceAsync(int serviceId)
    {
        return _httpClient.DeleteAsync($"api/admin/services/{serviceId}");
    }
}
