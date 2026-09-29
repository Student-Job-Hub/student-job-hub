namespace StudentJobHub.Api.Models;

public class JobApplication
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public Job? Job { get; set; }

    public string ApplicantId { get; set; } = string.Empty;

    public ApplicationUser? Applicant { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string? ResumeFileName { get; set; }

    public string? ResumeContentType { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}