namespace StudentJobHub.Client.Models;

public class ExportSummaryModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? University { get; set; }
    public DateTime ExportedAt { get; set; }

    public List<ApplicationModel> Applications { get; set; } = new();
    public List<JobModel> PostedJobs { get; set; } = new();
    public List<JobBookmarkModel> BookmarkedJobs { get; set; } = new();
    public List<ServiceBookingModel> ClientBookings { get; set; } = new();
    public List<ServiceBookingModel> ProviderBookings { get; set; } = new();
}
