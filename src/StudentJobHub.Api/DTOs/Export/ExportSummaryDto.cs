using StudentJobHub.Api.DTOs.Applications;
using StudentJobHub.Api.DTOs.Bookmarks;
using StudentJobHub.Api.DTOs.Bookings;
using StudentJobHub.Api.DTOs.Jobs;

namespace StudentJobHub.Api.DTOs.Export;

public class ExportSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? University { get; set; }
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    public List<ApplicationResponseDto> Applications { get; set; } = new();
    public List<JobResponseDto> PostedJobs { get; set; } = new();
    public List<JobBookmarkDto> BookmarkedJobs { get; set; } = new();
    public List<ServiceBookingResponseDto> ClientBookings { get; set; } = new();
    public List<ServiceBookingResponseDto> ProviderBookings { get; set; } = new();
}
