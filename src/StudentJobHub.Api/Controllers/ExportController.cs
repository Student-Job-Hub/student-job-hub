using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentJobHub.Api.Services;

namespace StudentJobHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ExportService _exportService;

    public ExportController(ExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("applications/csv")]
    public async Task<IActionResult> ExportApplicationsCsv()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var bytes = await _exportService.ExportApplicationsCsvAsync(userId);
        var filename = $"applications_history_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    [HttpGet("jobs/csv")]
    public async Task<IActionResult> ExportJobsCsv()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var bytes = await _exportService.ExportJobsCsvAsync(userId);
        var filename = $"posted_jobs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    [HttpGet("bookings/csv")]
    public async Task<IActionResult> ExportBookingsCsv()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var bytes = await _exportService.ExportBookingsCsvAsync(userId);
        var filename = $"service_bookings_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> ExportSummaryJson()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var summary = await _exportService.ExportUserDataSummaryAsync(userId);
        return Ok(summary);
    }

    [HttpGet("report/html")]
    public async Task<IActionResult> ExportReportHtml()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var html = await _exportService.GenerateHtmlReportAsync(userId);
        return Content(html, "text/html; charset=utf-8");
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
    }
}
