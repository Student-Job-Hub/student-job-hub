using System.Net.Http.Json;
using StudentJobHub.Client.Models;

namespace StudentJobHub.Client.Services;

public class BookmarkApiService
{
    private readonly HttpClient _httpClient;

    public BookmarkApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<JobBookmarkModel>> GetBookmarksAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<JobBookmarkModel>>("api/Bookmarks")
            ?? new List<JobBookmarkModel>();
    }

    public async Task<List<int>> GetBookmarkIdsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<int>>("api/Bookmarks/ids")
            ?? new List<int>();
    }

    public async Task<bool> IsBookmarkedAsync(int jobId)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<CheckResponse>($"api/Bookmarks/check/{jobId}");
            return res?.IsBookmarked ?? false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HttpResponseMessage> BookmarkJobAsync(int jobId)
    {
        return await _httpClient.PostAsync($"api/Bookmarks/{jobId}", null);
    }

    public async Task<HttpResponseMessage> RemoveBookmarkAsync(int jobId)
    {
        return await _httpClient.DeleteAsync($"api/Bookmarks/{jobId}");
    }

    private class CheckResponse
    {
        public bool IsBookmarked { get; set; }
    }
}
