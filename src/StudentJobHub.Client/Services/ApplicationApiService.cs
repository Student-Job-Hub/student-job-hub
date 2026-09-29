using System.Net.Http.Json;
using System.Net.Http.Headers;
using StudentJobHub.Client.Models;

namespace StudentJobHub.Client.Services;

public class ApplicationApiService
{
    private readonly HttpClient _httpClient;

    public ApplicationApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Submit a job application with an optional resume file.
    /// Uses multipart/form-data to support file upload.
    /// </summary>
    public async Task<HttpResponseMessage> ApplyAsync(
        int jobId,
        CreateApplicationModel model,
        Stream? resumeStream = null,
        string? resumeFileName = null)
    {
        using var content = new MultipartFormDataContent();

        content.Add(
            new StringContent(model.Message),
            "message");

        if (resumeStream != null && !string.IsNullOrEmpty(resumeFileName))
        {
            var fileContent = new StreamContent(resumeStream);

            // Set the content type based on file extension
            var extension = Path.GetExtension(resumeFileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(contentType);

            content.Add(fileContent, "resume", resumeFileName);
        }

        return await _httpClient.PostAsync(
            $"api/applications/{jobId}",
            content);
    }

    public async Task<List<ApplicationModel>> GetMyApplicationsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ApplicationModel>>(
            "api/applications/my")
            ?? new List<ApplicationModel>();
    }

    public async Task<List<ApplicationModel>> GetJobApplicationsAsync(int jobId)
    {
        return await _httpClient.GetFromJsonAsync<List<ApplicationModel>>(
            $"api/applications/job/{jobId}")
            ?? new List<ApplicationModel>();
    }

    public async Task<HttpResponseMessage> UpdateStatusAsync(
        int id,
        UpdateApplicationStatusModel model)
    {
        return await _httpClient.PatchAsJsonAsync(
            $"api/applications/{id}/status",
            model);
    }

    public async Task<ApplicationModel?> GetByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<ApplicationModel>(
            $"api/applications/{id}");
    }

    public async Task<HttpResponseMessage> DeleteAsync(int id)
    {
        return await _httpClient.DeleteAsync(
            $"api/applications/{id}");
    }

    /// <summary>
    /// Downloads a resume through the authorised HttpClient (sends the JWT).
    /// A plain link cannot be used because the API requires the Bearer token.
    /// Returns null if the resume is missing or the caller may not access it.
    /// </summary>
    public async Task<Stream?> GetResumeStreamAsync(int applicationId)
    {
        var response = await _httpClient.GetAsync(
            $"api/applications/{applicationId}/resume",
            HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            return null;
        }

        return await response.Content.ReadAsStreamAsync();
    }
}