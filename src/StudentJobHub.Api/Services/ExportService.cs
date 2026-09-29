using System.Text;
using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Applications;
using StudentJobHub.Api.DTOs.Bookmarks;
using StudentJobHub.Api.DTOs.Bookings;
using StudentJobHub.Api.DTOs.Export;
using StudentJobHub.Api.DTOs.Jobs;

namespace StudentJobHub.Api.Services;

public class ExportService
{
    private readonly ApplicationDbContext _context;

    public ExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportApplicationsCsvAsync(string userId)
    {
        var applications = await _context.JobApplications
            .Include(a => a.Job)
                .ThenInclude(j => j!.PostedBy)
            .Where(a => a.ApplicantId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        // UTF-8 BOM so Excel opens it with correct encoding
        sb.Append('\uFEFF');
        sb.AppendLine("Application ID,Job Title,Category,Budget (GHS),Posted By,Applied Date,Status,Your Pitch Message");

        foreach (var app in applications)
        {
            var jobTitle = CsvEscape(app.Job?.Title ?? "N/A");
            var category = CsvEscape(app.Job?.Category ?? "N/A");
            var budget = app.Job?.Budget.ToString("F2") ?? "0.00";
            var postedBy = CsvEscape(app.Job?.PostedBy?.FullName ?? "N/A");
            var appliedDate = app.AppliedAt.ToString("yyyy-MM-dd HH:mm:ss");
            var status = CsvEscape(app.Status);
            var message = CsvEscape(app.Message);

            sb.AppendLine($"{app.Id},{jobTitle},{category},{budget},{postedBy},{appliedDate},{status},{message}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportJobsCsvAsync(string userId)
    {
        var jobs = await _context.Jobs
            .Where(j => j.PostedById == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        var jobIds = jobs.Select(j => j.Id).ToList();
        var applicationCounts = await _context.JobApplications
            .Where(a => jobIds.Contains(a.JobId))
            .GroupBy(a => a.JobId)
            .Select(g => new { JobId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.JobId, v => v.Count);

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Job ID,Title,Category,Budget (GHS),Deadline,Status,Total Applications,Created Date,Requirements,Description");

        foreach (var job in jobs)
        {
            var title = CsvEscape(job.Title);
            var category = CsvEscape(job.Category);
            var budget = job.Budget.ToString("F2");
            var deadline = job.Deadline.ToString("yyyy-MM-dd");
            var status = job.IsOpen ? "Open" : "Closed";
            var appCount = applicationCounts.TryGetValue(job.Id, out var count) ? count : 0;
            var createdDate = job.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            var requirements = CsvEscape(job.Requirements);
            var description = CsvEscape(job.Description);

            sb.AppendLine($"{job.Id},{title},{category},{budget},{deadline},{status},{appCount},{createdDate},{requirements},{description}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportBookingsCsvAsync(string userId)
    {
        var bookings = await _context.ServiceBookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Include(b => b.Provider)
            .Where(b => b.ClientId == userId || b.ProviderId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Booking ID,Role,Service Title,Category,Other Party,Email,Requested Date,Location / Delivery,Proposed Price (GHS),Status,Client Notes,Provider Response,Created Date");

        foreach (var b in bookings)
        {
            var isClient = b.ClientId == userId;
            var role = isClient ? "Client (Requested)" : "Provider (Offered)";
            var serviceTitle = CsvEscape(b.Service?.Title ?? "N/A");
            var category = CsvEscape(b.Service?.Category ?? "N/A");
            var otherParty = CsvEscape(isClient ? (b.Provider?.FullName ?? "N/A") : (b.Client?.FullName ?? "N/A"));
            var email = CsvEscape(isClient ? (b.Provider?.Email ?? "N/A") : (b.Client?.Email ?? "N/A"));
            var requestedDate = b.RequestedDate.ToString("yyyy-MM-dd");
            var location = CsvEscape(b.LocationOrDelivery);
            var price = b.ProposedPrice.ToString("F2");
            var status = CsvEscape(b.Status);
            var notes = CsvEscape(b.Notes);
            var response = CsvEscape(b.ProviderResponse ?? string.Empty);
            var createdDate = b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

            sb.AppendLine($"{b.Id},{role},{serviceTitle},{category},{otherParty},{email},{requestedDate},{location},{price},{status},{notes},{response},{createdDate}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<ExportSummaryDto> ExportUserDataSummaryAsync(string userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        var applications = await _context.JobApplications
            .Include(a => a.Job)
            .Include(a => a.Applicant)
            .Where(a => a.ApplicantId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationResponseDto
            {
                Id = a.Id,
                JobId = a.JobId,
                JobTitle = a.Job != null ? a.Job.Title : string.Empty,
                ApplicantId = a.ApplicantId,
                ApplicantName = a.Applicant != null ? a.Applicant.FullName : string.Empty,
                Message = a.Message,
                Status = a.Status,
                AppliedAt = a.AppliedAt
            })
            .ToListAsync();

        var postedJobs = await _context.Jobs
            .Include(j => j.PostedBy)
            .Where(j => j.PostedById == userId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobResponseDto
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Category = j.Category,
                Requirements = j.Requirements,
                Budget = j.Budget,
                Deadline = j.Deadline,
                PostedById = j.PostedById,
                PostedByName = j.PostedBy != null ? j.PostedBy.FullName : string.Empty,
                IsOpen = j.IsOpen,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync();

        var bookmarks = await _context.JobBookmarks
            .Include(b => b.Job)
                .ThenInclude(j => j!.PostedBy)
            .Where(b => b.UserId == userId && b.Job != null)
            .OrderByDescending(b => b.SavedAt)
            .Select(b => new JobBookmarkDto
            {
                Id = b.Id,
                JobId = b.JobId,
                Title = b.Job!.Title,
                Description = b.Job.Description,
                Category = b.Job.Category,
                Requirements = b.Job.Requirements,
                Budget = b.Job.Budget,
                Deadline = b.Job.Deadline,
                PostedById = b.Job.PostedById,
                PostedByName = b.Job.PostedBy != null ? b.Job.PostedBy.FullName : string.Empty,
                IsOpen = b.Job.IsOpen,
                JobCreatedAt = b.Job.CreatedAt,
                SavedAt = b.SavedAt
            })
            .ToListAsync();

        var clientBookings = await _context.ServiceBookings
            .Include(b => b.Service)
            .Include(b => b.Provider)
            .Include(b => b.Client)
            .Where(b => b.ClientId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new ServiceBookingResponseDto
            {
                Id = b.Id,
                ServiceId = b.ServiceId,
                ServiceTitle = b.Service != null ? b.Service.Title : string.Empty,
                ServiceCategory = b.Service != null ? b.Service.Category : string.Empty,
                ClientId = b.ClientId,
                ClientName = b.Client != null ? b.Client.FullName : string.Empty,
                ClientEmail = b.Client != null ? b.Client.Email ?? string.Empty : string.Empty,
                ProviderId = b.ProviderId,
                ProviderName = b.Provider != null ? b.Provider.FullName : string.Empty,
                ProviderEmail = b.Provider != null ? b.Provider.Email ?? string.Empty : string.Empty,
                RequestedDate = b.RequestedDate,
                LocationOrDelivery = b.LocationOrDelivery,
                Notes = b.Notes,
                ProposedPrice = b.ProposedPrice,
                Status = b.Status,
                ProviderResponse = b.ProviderResponse,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync();

        var providerBookings = await _context.ServiceBookings
            .Include(b => b.Service)
            .Include(b => b.Provider)
            .Include(b => b.Client)
            .Where(b => b.ProviderId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new ServiceBookingResponseDto
            {
                Id = b.Id,
                ServiceId = b.ServiceId,
                ServiceTitle = b.Service != null ? b.Service.Title : string.Empty,
                ServiceCategory = b.Service != null ? b.Service.Category : string.Empty,
                ClientId = b.ClientId,
                ClientName = b.Client != null ? b.Client.FullName : string.Empty,
                ClientEmail = b.Client != null ? b.Client.Email ?? string.Empty : string.Empty,
                ClientUniversity = b.Client != null ? b.Client.University : null,
                ProviderId = b.ProviderId,
                ProviderName = b.Provider != null ? b.Provider.FullName : string.Empty,
                ProviderEmail = b.Provider != null ? b.Provider.Email ?? string.Empty : string.Empty,
                RequestedDate = b.RequestedDate,
                LocationOrDelivery = b.LocationOrDelivery,
                Notes = b.Notes,
                ProposedPrice = b.ProposedPrice,
                Status = b.Status,
                ProviderResponse = b.ProviderResponse,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync();

        return new ExportSummaryDto
        {
            UserId = userId,
            FullName = user?.FullName ?? "User",
            Email = user?.Email ?? string.Empty,
            University = user?.University,
            ExportedAt = DateTime.UtcNow,
            Applications = applications,
            PostedJobs = postedJobs,
            BookmarkedJobs = bookmarks,
            ClientBookings = clientBookings,
            ProviderBookings = providerBookings
        };
    }

    public async Task<string> GenerateHtmlReportAsync(string userId)
    {
        var summary = await ExportUserDataSummaryAsync(userId);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <title>Student Job Hub - Career & Activity Report</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 30px; color: #1e293b; background: #fff; }");
        sb.AppendLine("    .header { border-bottom: 2px solid #2563eb; padding-bottom: 16px; margin-bottom: 24px; display: flex; justify-content: space-between; align-items: flex-end; }");
        sb.AppendLine("    .header h1 { margin: 0; color: #1e40af; font-size: 24px; }");
        sb.AppendLine("    .header p { margin: 4px 0 0; color: #64748b; font-size: 14px; }");
        sb.AppendLine("    .user-info { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin-bottom: 24px; display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }");
        sb.AppendLine("    .stat-box { display: flex; flex-direction: column; }");
        sb.AppendLine("    .stat-label { font-size: 11px; text-transform: uppercase; color: #64748b; font-weight: 600; }");
        sb.AppendLine("    .stat-value { font-size: 15px; font-weight: 700; color: #0f172a; margin-top: 2px; }");
        sb.AppendLine("    h2 { color: #0f172a; font-size: 18px; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px; margin-top: 32px; margin-bottom: 16px; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-bottom: 24px; font-size: 13px; }");
        sb.AppendLine("    th { background: #f1f5f9; text-align: left; padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: 600; color: #334155; }");
        sb.AppendLine("    td { padding: 10px 12px; border: 1px solid #e2e8f0; vertical-align: top; }");
        sb.AppendLine("    tr:nth-child(even) { background: #f8fafc; }");
        sb.AppendLine("    .badge { display: inline-block; padding: 3px 8px; border-radius: 12px; font-size: 11px; font-weight: 600; }");
        sb.AppendLine("    .badge-pending { background: #fef3c7; color: #92400e; }");
        sb.AppendLine("    .badge-accepted { background: #dcfce7; color: #166534; }");
        sb.AppendLine("    .badge-rejected { background: #fee2e2; color: #991b1b; }");
        sb.AppendLine("    .badge-completed { background: #dbeafe; color: #1e40af; }");
        sb.AppendLine("    .badge-open { background: #dcfce7; color: #166534; }");
        sb.AppendLine("    .badge-closed { background: #fee2e2; color: #991b1b; }");
        sb.AppendLine("    .footer { margin-top: 40px; padding-top: 16px; border-top: 1px solid #cbd5e1; font-size: 12px; color: #94a3b8; text-align: center; }");
        sb.AppendLine("    @media print { body { margin: 15mm; } .no-print { display: none; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("  <div class=\"no-print\" style=\"background: #eff6ff; border: 1px solid #bfdbfe; padding: 12px; border-radius: 6px; margin-bottom: 20px; display: flex; justify-content: space-between; align-items: center;\">");
        sb.AppendLine("    <span style=\"color: #1e40af; font-weight: 600;\">Student Job Hub Official Activity Export</span>");
        sb.AppendLine("    <button onclick=\"window.print()\" style=\"background: #2563eb; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600;\">Print to PDF / Print</button>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <div class=\"header\">");
        sb.AppendLine("    <div>");
        sb.AppendLine("      <h1>Student Job Hub — Activity & Career Report</h1>");
        sb.AppendLine($"      <p>Generated on {summary.ExportedAt:MMMM dd, yyyy HH:mm} UTC</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <div class=\"user-info\">");
        sb.AppendLine($"    <div class=\"stat-box\"><span class=\"stat-label\">Account Name</span><span class=\"stat-value\">{Escape(summary.FullName)}</span></div>");
        sb.AppendLine($"    <div class=\"stat-box\"><span class=\"stat-label\">Email Address</span><span class=\"stat-value\">{Escape(summary.Email)}</span></div>");
        sb.AppendLine($"    <div class=\"stat-box\"><span class=\"stat-label\">University / Institution</span><span class=\"stat-value\">{Escape(summary.University ?? "N/A")}</span></div>");
        sb.AppendLine("  </div>");

        // Section 1: Applications
        sb.AppendLine($"  <h2>1. Job Applications History ({summary.Applications.Count})</h2>");
        if (summary.Applications.Count == 0)
        {
            sb.AppendLine("  <p style=\"color: #64748b;\">No job applications recorded.</p>");
        }
        else
        {
            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead><tr><th>Job Title</th><th>Applied Date</th><th>Status</th><th>Pitch / Message</th></tr></thead>");
            sb.AppendLine("    <tbody>");
            foreach (var app in summary.Applications)
            {
                var badgeClass = app.Status.ToLower() switch
                {
                    "accepted" => "badge-accepted",
                    "rejected" => "badge-rejected",
                    _ => "badge-pending"
                };
                sb.AppendLine($"      <tr><td><strong>{Escape(app.JobTitle)}</strong></td><td>{app.AppliedAt:MMM dd, yyyy}</td><td><span class=\"badge {badgeClass}\">{Escape(app.Status)}</span></td><td>{Escape(app.Message)}</td></tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
        }

        // Section 2: Posted Jobs
        sb.AppendLine($"  <h2>2. Posted Jobs History ({summary.PostedJobs.Count})</h2>");
        if (summary.PostedJobs.Count == 0)
        {
            sb.AppendLine("  <p style=\"color: #64748b;\">No jobs posted.</p>");
        }
        else
        {
            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead><tr><th>Job Title</th><th>Category</th><th>Budget</th><th>Deadline</th><th>Status</th><th>Posted Date</th></tr></thead>");
            sb.AppendLine("    <tbody>");
            foreach (var job in summary.PostedJobs)
            {
                var badgeClass = job.IsOpen ? "badge-open" : "badge-closed";
                sb.AppendLine($"      <tr><td><strong>{Escape(job.Title)}</strong></td><td>{Escape(job.Category)}</td><td>GH₵ {job.Budget:N2}</td><td>{job.Deadline:MMM dd, yyyy}</td><td><span class=\"badge {badgeClass}\">{(job.IsOpen ? "Open" : "Closed")}</span></td><td>{job.CreatedAt:MMM dd, yyyy}</td></tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
        }

        // Section 3: Service Bookings Requested
        sb.AppendLine($"  <h2>3. Service Requests Made ({summary.ClientBookings.Count})</h2>");
        if (summary.ClientBookings.Count == 0)
        {
            sb.AppendLine("  <p style=\"color: #64748b;\">No service bookings requested.</p>");
        }
        else
        {
            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead><tr><th>Service</th><th>Provider</th><th>Requested Date</th><th>Price</th><th>Status</th><th>Notes</th></tr></thead>");
            sb.AppendLine("    <tbody>");
            foreach (var b in summary.ClientBookings)
            {
                var badgeClass = b.Status.ToLower() switch
                {
                    "accepted" => "badge-accepted",
                    "completed" => "badge-completed",
                    "declined" => "badge-rejected",
                    _ => "badge-pending"
                };
                sb.AppendLine($"      <tr><td><strong>{Escape(b.ServiceTitle)}</strong></td><td>{Escape(b.ProviderName)}</td><td>{b.RequestedDate:MMM dd, yyyy}</td><td>GH₵ {b.ProposedPrice:N2}</td><td><span class=\"badge {badgeClass}\">{Escape(b.Status)}</span></td><td>{Escape(b.Notes)}</td></tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
        }

        // Section 4: Service Bookings Received
        sb.AppendLine($"  <h2>4. Service Booking Requests Received ({summary.ProviderBookings.Count})</h2>");
        if (summary.ProviderBookings.Count == 0)
        {
            sb.AppendLine("  <p style=\"color: #64748b;\">No booking requests received for services.</p>");
        }
        else
        {
            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead><tr><th>Service</th><th>Client</th><th>Requested Date</th><th>Price</th><th>Status</th><th>Client Notes</th></tr></thead>");
            sb.AppendLine("    <tbody>");
            foreach (var b in summary.ProviderBookings)
            {
                var badgeClass = b.Status.ToLower() switch
                {
                    "accepted" => "badge-accepted",
                    "completed" => "badge-completed",
                    "declined" => "badge-rejected",
                    _ => "badge-pending"
                };
                sb.AppendLine($"      <tr><td><strong>{Escape(b.ServiceTitle)}</strong></td><td>{Escape(b.ClientName)}</td><td>{b.RequestedDate:MMM dd, yyyy}</td><td>GH₵ {b.ProposedPrice:N2}</td><td><span class=\"badge {badgeClass}\">{Escape(b.Status)}</span></td><td>{Escape(b.Notes)}</td></tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
        }

        // Section 5: Saved Jobs
        sb.AppendLine($"  <h2>5. Bookmarked Jobs ({summary.BookmarkedJobs.Count})</h2>");
        if (summary.BookmarkedJobs.Count == 0)
        {
            sb.AppendLine("  <p style=\"color: #64748b;\">No saved bookmarks.</p>");
        }
        else
        {
            sb.AppendLine("  <table>");
            sb.AppendLine("    <thead><tr><th>Job Title</th><th>Category</th><th>Budget</th><th>Posted By</th><th>Deadline</th><th>Saved Date</th></tr></thead>");
            sb.AppendLine("    <tbody>");
            foreach (var bm in summary.BookmarkedJobs)
            {
                sb.AppendLine($"      <tr><td><strong>{Escape(bm.Title)}</strong></td><td>{Escape(bm.Category)}</td><td>GH₵ {bm.Budget:N2}</td><td>{Escape(bm.PostedByName)}</td><td>{bm.Deadline:MMM dd, yyyy}</td><td>{bm.SavedAt:MMM dd, yyyy}</td></tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
        }

        sb.AppendLine("  <div class=\"footer\">");
        sb.AppendLine("    Student Job Hub Platform &copy; 2026 — Verified Student Career Activity Document");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return System.Net.WebUtility.HtmlEncode(value);
    }
}
