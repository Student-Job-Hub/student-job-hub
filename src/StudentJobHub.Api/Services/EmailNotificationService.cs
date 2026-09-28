using System.Net;
using System.Net.Mail;

namespace StudentJobHub.Api.Services;

/// <summary>
/// Sends email notifications (Feature #25): application status changes and
/// new-application alerts for job owners.
/// SMTP settings come from the "Email" section of configuration.
/// If SMTP is not configured the email is only logged (development mode).
/// Email failures never break the calling request.
/// </summary>
public class EmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // =========================================================
    // SEND STATUS CHANGE EMAIL
    // =========================================================

    public async Task SendStatusChangeEmailAsync(
        string recipientEmail,
        string recipientName,
        string jobTitle,
        string newStatus)
    {
        var subject = $"Application Update: {StripLineBreaks(jobTitle)}";

        var statusMessage = newStatus.ToLowerInvariant() switch
        {
            "accepted" => "Congratulations! Your application has been accepted.",
            "rejected" => "Unfortunately, your application was not selected this time.",
            "pending" => "Your application status has been updated to pending.",
            _ => $"Your application status has been updated to: {newStatus}."
        };

        var content = $@"
                <p style=""margin: 0 0 16px; font-size: 16px; color: #1f2937;"">Hi <strong>{Encode(recipientName)}</strong>,</p>
                <p style=""margin: 0 0 24px; font-size: 15px; color: #4b5563; line-height: 1.6;"">
                    We have an update regarding your application for <strong style=""color: #1f2937;"">{Encode(jobTitle)}</strong>.
                </p>
                <div style=""background-color: #f0f4ff; border-left: 4px solid #2563eb; padding: 16px 20px; border-radius: 0 8px 8px 0; margin-bottom: 24px;"">
                    <p style=""margin: 0 0 4px; font-size: 13px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px;"">Status</p>
                    <p style=""margin: 0; font-size: 18px; font-weight: 600; color: #2563eb;"">{Encode(newStatus)}</p>
                </div>
                <p style=""margin: 0 0 24px; font-size: 15px; color: #4b5563; line-height: 1.6;"">{Encode(statusMessage)}</p>
                <p style=""margin: 0; font-size: 14px; color: #9ca3af;"">Log in to your account to view more details.</p>";

        await SendEmailAsync(
            recipientEmail,
            subject,
            BuildLayout("Application Status Update", content));
    }

    // =========================================================
    // SEND APPLICATION RECEIVED EMAIL
    // =========================================================

    public async Task SendApplicationReceivedEmailAsync(
        string jobOwnerEmail,
        string jobOwnerName,
        string jobTitle,
        string applicantName)
    {
        var subject = $"New Application: {StripLineBreaks(jobTitle)}";

        var content = $@"
                <p style=""margin: 0 0 16px; font-size: 16px; color: #1f2937;"">Hi <strong>{Encode(jobOwnerName)}</strong>,</p>
                <p style=""margin: 0 0 24px; font-size: 15px; color: #4b5563; line-height: 1.6;"">
                    <strong style=""color: #1f2937;"">{Encode(applicantName)}</strong> has submitted an application for your job posting:
                    <strong style=""color: #1f2937;"">{Encode(jobTitle)}</strong>.
                </p>
                <p style=""margin: 0 0 24px; font-size: 15px; color: #4b5563; line-height: 1.6;"">
                    Log in to review the application and respond to the applicant.
                </p>";

        await SendEmailAsync(
            jobOwnerEmail,
            subject,
            BuildLayout("New Application Received", content));
    }

    // =========================================================
    // HELPERS
    // =========================================================

    // User-controlled text (names, job titles) must be HTML-encoded so it
    // cannot inject markup into the email body.
    private static string Encode(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);

    // Subjects must not contain CR/LF (header injection).
    private static string StripLineBreaks(string? value) =>
        (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");

    private static string BuildLayout(string heading, string innerHtml) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; margin: 30px auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);"">
        <tr>
            <td style=""background: linear-gradient(135deg, #2563eb, #1e40af); padding: 32px 24px; text-align: center;"">
                <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: 700;"">Student Job Hub</h1>
                <p style=""color: rgba(255,255,255,0.85); margin: 8px 0 0; font-size: 14px;"">{Encode(heading)}</p>
            </td>
        </tr>
        <tr>
            <td style=""padding: 32px 24px;"">{innerHtml}
            </td>
        </tr>
        <tr>
            <td style=""background-color: #f9fafb; padding: 20px 24px; text-align: center; border-top: 1px solid #e5e7eb;"">
                <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">
                    &copy; {DateTime.UtcNow.Year} Student Job Hub. All rights reserved.
                </p>
            </td>
        </tr>
    </table>
</body>
</html>";

    // =========================================================
    // CORE SEND EMAIL METHOD
    // =========================================================

    private async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPortStr = _configuration["Email:SmtpPort"];
        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPass = _configuration["Email:SmtpPass"];
        var fromEmail = _configuration["Email:FromEmail"];
        var fromName = _configuration["Email:FromName"];

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            fromEmail = "noreply@studentjobhub.com";
        }

        if (string.IsNullOrWhiteSpace(fromName))
        {
            fromName = "Student Job Hub";
        }

        // =======================================================
        // If SMTP is not configured, log the email instead
        // =======================================================

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            !int.TryParse(smtpPortStr, out var port))
        {
            _logger.LogInformation(
                "[Email] SMTP not configured. Would have sent email to {To}: {Subject}",
                to,
                subject);
            return;
        }

        // SSL is on by default; set Email:EnableSsl=false only for local test servers.
        var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) || ssl;

        try
        {
            using var client = new SmtpClient(smtpHost, port)
            {
                EnableSsl = enableSsl,
                Timeout = 10_000
            };

            if (!string.IsNullOrWhiteSpace(smtpUser))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation(
                "[Email] Successfully sent email to {To}: {Subject}",
                to,
                subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[Email] Failed to send email to {To}: {Subject}",
                to,
                subject);

            // Don't throw — email failures should not break application flow
        }
    }
}
