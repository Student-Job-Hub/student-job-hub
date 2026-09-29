using System.Net.Http.Json;
using StudentJobHub.Client.Models;

namespace StudentJobHub.Client.Services;

public class BookingApiService
{
    private readonly HttpClient _httpClient;

    public BookingApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> CreateBookingAsync(CreateServiceBookingModel model)
    {
        return await _httpClient.PostAsJsonAsync("api/Bookings", model);
    }

    public async Task<List<ServiceBookingModel>> GetMyRequestsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ServiceBookingModel>>("api/Bookings/my-requests")
            ?? new List<ServiceBookingModel>();
    }

    public async Task<List<ServiceBookingModel>> GetReceivedBookingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ServiceBookingModel>>("api/Bookings/received")
            ?? new List<ServiceBookingModel>();
    }

    public async Task<ServiceBookingModel?> GetByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<ServiceBookingModel>($"api/Bookings/{id}");
    }

    public async Task<HttpResponseMessage> UpdateStatusAsync(int id, UpdateBookingStatusModel model)
    {
        return await _httpClient.PatchAsJsonAsync($"api/Bookings/{id}/status", model);
    }

    public async Task<HttpResponseMessage> DeleteBookingAsync(int id)
    {
        return await _httpClient.DeleteAsync($"api/Bookings/{id}");
    }
}
