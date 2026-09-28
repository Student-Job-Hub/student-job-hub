namespace StudentJobHub.Client.Models;

public class ApplicationModel
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string ApplicantId { get; set; } = string.Empty;

    public string ApplicantName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string? ResumeFileName { get; set; }

    public string? ResumeDownloadUrl { get; set; }

    public DateTime AppliedAt { get; set; }
}