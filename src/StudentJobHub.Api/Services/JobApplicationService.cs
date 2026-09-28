using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Applications;
using StudentJobHub.Api.Hubs;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Services;

public class JobApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly EmailNotificationService _emailService;
    private readonly string _resumeUploadPath;

    public JobApplicationService(
        ApplicationDbContext context,
        NotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        EmailNotificationService emailService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _notificationService = notificationService;
        _hubContext = hubContext;
        _emailService = emailService;
        // Stored OUTSIDE wwwroot so files are never served by UseStaticFiles();
        // they can only be fetched through the authorised download endpoint.
        _resumeUploadPath = Path.Combine(
            environment.ContentRootPath, "App_Data", "resumes");
    }

    // =========================================================
    // CREATE APPLICATION (with optional resume)
    // =========================================================

    public async Task<(bool Success, string Message, ApplicationResponseDto? Application)>
        CreateAsync(
            int jobId,
            CreateApplicationDto dto,
            string applicantId,
            IFormFile? resume = null)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            return (false, "Job not found.", null);
        }

        if (!job.IsOpen)
        {
            return (false, "This job is no longer open.", null);
        }

        if (job.PostedById == applicantId)
        {
            return (false, "You cannot apply to your own job.", null);
        }

        var existingApplication = await _context.JobApplications
            .AnyAsync(a =>
                a.JobId == jobId &&
                a.ApplicantId == applicantId);

        if (existingApplication)
        {
            return (
                false,
                "You have already applied to this job.",
                null);
        }

        // =====================================================
        // VALIDATE RESUME FILE (if provided)
        // =====================================================

        if (resume is { Length: > 0 })
        {
            var validation = await ResumeFileRules.ValidateAsync(resume);

            if (!validation.IsValid)
            {
                return (false, validation.Error, null);
            }
        }

        var application = new JobApplication
        {
            JobId = jobId,
            ApplicantId = applicantId,
            Message = dto.Message,
            Status = "Pending"
        };

        if (resume is { Length: > 0 })
        {
            // Content type is derived from the validated extension, not the client header.
            application.ResumeFileName = ResumeFileRules.SanitizeFileName(resume.FileName);
            application.ResumeContentType = ResumeFileRules.GetContentType(resume.FileName);
        }

        _context.JobApplications.Add(application);

        await _context.SaveChangesAsync();

        // =====================================================
        // SAVE RESUME FILE TO DISK (using application ID)
        // =====================================================

        if (resume is { Length: > 0 })
        {
            try
            {
                Directory.CreateDirectory(_resumeUploadPath);

                var extension = Path.GetExtension(application.ResumeFileName);
                var savedFileName = $"{application.Id}{extension}";
                var filePath = Path.Combine(_resumeUploadPath, savedFileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await resume.CopyToAsync(stream);
            }
            catch (Exception)
            {
                // Do not leave an application pointing at a resume that was never saved.
                _context.JobApplications.Remove(application);
                await _context.SaveChangesAsync();

                return (false, "Could not save the resume file. Please try again.", null);
            }
        }

        // =====================================================
        // CREATE NOTIFICATION FOR JOB OWNER
        // =====================================================

        var notification = await _notificationService.CreateAsync(
            job.PostedById,
            $"A new application was submitted for your job: {job.Title}");

        // =====================================================
        // SEND REAL-TIME NOTIFICATION
        // =====================================================

        await _hubContext.Clients
            .Group($"user-{job.PostedById}")
            .SendAsync(
                "ReceiveNotification",
                new
                {
                    notification.Id,
                    notification.Message,
                    notification.IsRead,
                    notification.CreatedAt
                });

        // =====================================================
        // SEND EMAIL TO JOB OWNER (Feature #25)
        // =====================================================

        var jobOwner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == job.PostedById);

        if (!string.IsNullOrWhiteSpace(jobOwner?.Email))
        {
            var applicantUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == applicantId);

            await _emailService.SendApplicationReceivedEmailAsync(
                jobOwner.Email,
                jobOwner.FullName,
                job.Title,
                applicantUser?.FullName ?? "A student");
        }

        var response = await GetByIdAsync(application.Id);

        return (
            true,
            "Application submitted successfully.",
            response);
    }

    // =========================================================
    // GET MY APPLICATIONS
    // =========================================================

    public async Task<List<ApplicationResponseDto>> GetMyApplicationsAsync(
        string applicantId)
    {
        return await _context.JobApplications
            .Include(a => a.Job)
            .Include(a => a.Applicant)
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationResponseDto
            {
                Id = a.Id,
                JobId = a.JobId,
                JobTitle = a.Job != null
                    ? a.Job.Title
                    : string.Empty,
                ApplicantId = a.ApplicantId,
                ApplicantName = a.Applicant != null
                    ? a.Applicant.FullName
                    : string.Empty,
                Message = a.Message,
                Status = a.Status,
                ResumeFileName = a.ResumeFileName,
                ResumeDownloadUrl = a.ResumeFileName != null
                    ? $"/api/applications/{a.Id}/resume"
                    : null,
                AppliedAt = a.AppliedAt
            })
            .ToListAsync();
    }

    // =========================================================
    // GET APPLICATIONS FOR A JOB
    // =========================================================

    public async Task<(bool Success, string Message, List<ApplicationResponseDto>? Applications)>
        GetJobApplicationsAsync(
            int jobId,
            string userId)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            return (false, "Job not found.", null);
        }

        if (job.PostedById != userId)
        {
            return (
                false,
                "You are not authorized to view these applications.",
                null);
        }

        var applications = await _context.JobApplications
            .Include(a => a.Job)
            .Include(a => a.Applicant)
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationResponseDto
            {
                Id = a.Id,
                JobId = a.JobId,
                JobTitle = a.Job != null
                    ? a.Job.Title
                    : string.Empty,
                ApplicantId = a.ApplicantId,
                ApplicantName = a.Applicant != null
                    ? a.Applicant.FullName
                    : string.Empty,
                Message = a.Message,
                Status = a.Status,
                ResumeFileName = a.ResumeFileName,
                ResumeDownloadUrl = a.ResumeFileName != null
                    ? $"/api/applications/{a.Id}/resume"
                    : null,
                AppliedAt = a.AppliedAt
            })
            .ToListAsync();

        return (true, string.Empty, applications);
    }

    // =========================================================
    // UPDATE APPLICATION STATUS
    // =========================================================

    public async Task<(bool Success, string Message)>
        UpdateStatusAsync(
            int applicationId,
            string status,
            string userId)
    {
        var application = await _context.JobApplications
            .Include(a => a.Job)
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
        {
            return (false, "Application not found.");
        }

        if (application.Job == null ||
            application.Job.PostedById != userId)
        {
            return (
                false,
                "You are not authorized to update this application.");
        }

        var normalizedStatus = status.Trim();

        var allowedStatuses = new[]
        {
            "Pending",
            "Accepted",
            "Rejected"
        };

        var validStatus = allowedStatuses
            .FirstOrDefault(s =>
                s.Equals(
                    normalizedStatus,
                    StringComparison.OrdinalIgnoreCase));

        if (validStatus == null)
        {
            return (
                false,
                "Invalid status. Use Pending, Accepted, or Rejected.");
        }

        if (string.Equals(application.Status, validStatus, StringComparison.OrdinalIgnoreCase))
        {
            // Nothing changed, so don't notify or email the applicant again.
            return (true, "Application status is already " + validStatus + ".");
        }

        application.Status = validStatus;

        await _context.SaveChangesAsync();

        // =====================================================
        // NOTIFY APPLICANT (in-app)
        // =====================================================

        var notification = await _notificationService.CreateAsync(
            application.ApplicantId,
            $"Your application for '{application.Job.Title}' was {validStatus.ToLower()}.");

        // =====================================================
        // SEND REAL-TIME NOTIFICATION
        // =====================================================

        await _hubContext.Clients
            .Group($"user-{application.ApplicantId}")
            .SendAsync(
                "ReceiveNotification",
                new
                {
                    notification.Id,
                    notification.Message,
                    notification.IsRead,
                    notification.CreatedAt
                });

        // =====================================================
        // SEND EMAIL NOTIFICATION (Feature #25)
        // =====================================================

        if (!string.IsNullOrWhiteSpace(application.Applicant?.Email))
        {
            await _emailService.SendStatusChangeEmailAsync(
                application.Applicant!.Email!,
                application.Applicant.FullName,
                application.Job.Title,
                validStatus);
        }

        return (
            true,
            "Application status updated successfully.");
    }

    // =========================================================
    // DELETE APPLICATION
    // =========================================================

    public async Task<(bool Success, string Message)>
        DeleteAsync(
            int applicationId,
            string applicantId)
    {
        var application = await _context.JobApplications
            .FirstOrDefaultAsync(a =>
                a.Id == applicationId &&
                a.ApplicantId == applicantId);

        if (application == null)
        {
            return (
                false,
                "Application not found or you are not the applicant.");
        }

        if (!application.Status.Equals(
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            return (
                false,
                "Only pending applications can be deleted.");
        }

        // Delete resume file if it exists
        if (!string.IsNullOrEmpty(application.ResumeFileName))
        {
            DeleteResumeFile(application.Id, application.ResumeFileName);
        }

        _context.JobApplications.Remove(application);

        await _context.SaveChangesAsync();

        return (
            true,
            "Application deleted successfully.");
    }

    // =========================================================
    // GET APPLICATION BY ID
    // =========================================================

    public async Task<ApplicationResponseDto?> GetByIdAsync(
        int applicationId)
    {
        return await _context.JobApplications
            .Include(a => a.Job)
            .Include(a => a.Applicant)
            .Where(a => a.Id == applicationId)
            .Select(a => new ApplicationResponseDto
            {
                Id = a.Id,
                JobId = a.JobId,
                JobTitle = a.Job != null
                    ? a.Job.Title
                    : string.Empty,
                ApplicantId = a.ApplicantId,
                ApplicantName = a.Applicant != null
                    ? a.Applicant.FullName
                    : string.Empty,
                Message = a.Message,
                Status = a.Status,
                ResumeFileName = a.ResumeFileName,
                ResumeDownloadUrl = a.ResumeFileName != null
                    ? $"/api/applications/{a.Id}/resume"
                    : null,
                AppliedAt = a.AppliedAt
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // GET RESUME FILE
    // =========================================================

    public async Task<(Stream FileStream, string ContentType, string FileName)?> GetResumeAsync(
        int applicationId)
    {
        var application = await _context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application?.ResumeFileName == null ||
            application.ResumeContentType == null)
        {
            return null;
        }

        var extension = Path.GetExtension(application.ResumeFileName);
        var savedFileName = $"{applicationId}{extension}";
        var filePath = Path.Combine(_resumeUploadPath, savedFileName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return (stream, application.ResumeContentType, application.ResumeFileName);
    }

    // =========================================================
    // HELPER: Delete resume file
    // =========================================================

    private void DeleteResumeFile(int applicationId, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var savedFileName = $"{applicationId}{extension}";
        var filePath = Path.Combine(_resumeUploadPath, savedFileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}