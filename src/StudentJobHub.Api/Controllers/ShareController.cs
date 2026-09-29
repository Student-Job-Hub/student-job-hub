using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;

namespace StudentJobHub.Api.Controllers;

/// <summary>
/// Feature #30 (SEO &amp; Open Graph): social crawlers (WhatsApp, Facebook, X, LinkedIn)
/// do not run JavaScript, so they never see the tags a Blazor WebAssembly page sets
/// at runtime. These endpoints return a tiny server-rendered page carrying the real
/// Open Graph tags for a job/service and then send humans on to the client app.
/// Share links look like:  {api}/share/jobs/5  and  {api}/share/services/3
/// </summary>
[ApiController]
[Route("share")]
[AllowAnonymous]
public class ShareController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly string _clientBaseUrl;

    public ShareController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;

        var configured = configuration["Client:BaseUrl"];

        _clientBaseUrl = (string.IsNullOrWhiteSpace(configured)
            ? "http://localhost:5289"
            : configured).TrimEnd('/');
    }

    [HttpGet("jobs/{id:int}")]
    public async Task<IActionResult> Job(int id)
    {
        var job = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new { j.Title, j.Description, j.Budget, j.Deadline })
            .FirstOrDefaultAsync();

        if (job == null)
        {
            return NotFound();
        }

        var description =
            $"Budget: GH₵ {job.Budget:N2} — Deadline: {job.Deadline:dd MMM yyyy}. {job.Description}";

        return Html(BuildPage(
            job.Title,
            description,
            $"{_clientBaseUrl}/jobs/{id}"));
    }

    [HttpGet("services/{id:int}")]
    public async Task<IActionResult> Service(int id)
    {
        var service = await _context.Services
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Title, s.Description, s.Price })
            .FirstOrDefaultAsync();

        if (service == null)
        {
            return NotFound();
        }

        var description =
            $"Price: GH₵ {service.Price:N2}. {service.Description}";

        return Html(BuildPage(
            service.Title,
            description,
            $"{_clientBaseUrl}/services/{id}"));
    }

    private ContentResult Html(string html) => new()
    {
        Content = html,
        ContentType = "text/html; charset=utf-8",
        StatusCode = 200
    };

    private string BuildPage(string title, string description, string targetUrl)
    {
        var fullTitle = WebUtility.HtmlEncode($"{title} | Student Job Hub");
        var desc = WebUtility.HtmlEncode(Truncate(description, 200));
        var url = WebUtility.HtmlEncode(targetUrl);
        var image = WebUtility.HtmlEncode($"{_clientBaseUrl}/og-image.png");

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <title>{fullTitle}</title>
    <meta name=""description"" content=""{desc}"" />
    <link rel=""canonical"" href=""{url}"" />
    <meta property=""og:type"" content=""article"" />
    <meta property=""og:site_name"" content=""Student Job Hub"" />
    <meta property=""og:title"" content=""{fullTitle}"" />
    <meta property=""og:description"" content=""{desc}"" />
    <meta property=""og:url"" content=""{url}"" />
    <meta property=""og:image"" content=""{image}"" />
    <meta property=""og:image:width"" content=""1200"" />
    <meta property=""og:image:height"" content=""630"" />
    <meta name=""twitter:card"" content=""summary_large_image"" />
    <meta name=""twitter:title"" content=""{fullTitle}"" />
    <meta name=""twitter:description"" content=""{desc}"" />
    <meta name=""twitter:image"" content=""{image}"" />
    <meta http-equiv=""refresh"" content=""0; url={url}"" />
</head>
<body>
    <p>Redirecting to <a href=""{url}"">{fullTitle}</a>…</p>
</body>
</html>";
    }

    private static string Truncate(string text, int max)
    {
        var clean = string.Join(' ', (text ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return clean.Length <= max ? clean : clean[..(max - 3)].TrimEnd() + "...";
    }
}
