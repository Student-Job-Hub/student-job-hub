using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentJobHub.Api.DTOs.Applications;
using StudentJobHub.Api.Services;

namespace StudentJobHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly JobApplicationService _applicationService;

    public ApplicationsController(
        JobApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    // ==========================================
    // APPLY TO A JOB (with optional resume)
    // =========================================

    [HttpPost("{jobId:int}")]
    [RequestSizeLimit(ResumeFileRules.MaxSizeBytes + 512 * 1024)]
    public async Task<IActionResult> Create(
        int jobId,
        [FromForm] CreateApplicationDto dto,
        IFormFile? resume = null)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _applicationService.CreateAsync(
            jobId,
            dto,
            userId,
            resume);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Application!.Id },
            result.Application);
    }

    // ==========================================
    // GET MY APPLICATIONS
    // ==========================================

    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var applications =
            await _applicationService.GetMyApplicationsAsync(userId);

        return Ok(applications);
    }

    // ==========================================
    // GET APPLICATIONS FOR A JOB
    // ==========================================

    [HttpGet("job/{jobId:int}")]
    public async Task<IActionResult> GetJobApplications(
        int jobId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result =
            await _applicationService.GetJobApplicationsAsync(
                jobId,
                userId);

        if (!result.Success)
        {
            return result.Message == "Job not found."
                ? NotFound(new { message = result.Message })
                : Forbid();
        }

        return Ok(result.Applications);
    }

    // ==========================================
    // GET APPLICATION BY ID
    // ==========================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var application =
            await _applicationService.GetByIdAsync(id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        // Only applicant or job owner can view it.
        var job = await _applicationService
            .GetJobApplicationsAsync(
                application.JobId,
                userId);

        var isJobOwner = job.Success;

        if (application.ApplicantId != userId &&
            !isJobOwner)
        {
            return Forbid();
        }

        return Ok(application);
    }

    // ==========================================
    // DOWNLOAD RESUME
    // ==========================================

    [HttpGet("{id:int}/resume")]
    public async Task<IActionResult> DownloadResume(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        // Verify the user has access (applicant or job owner)
        var application =
            await _applicationService.GetByIdAsync(id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        var jobCheck = await _applicationService
            .GetJobApplicationsAsync(
                application.JobId,
                userId);

        var isJobOwner = jobCheck.Success;

        if (application.ApplicantId != userId &&
            !isJobOwner)
        {
            return Forbid();
        }

        var resumeResult = await _applicationService.GetResumeAsync(id);

        if (resumeResult == null)
        {
            return NotFound(new
            {
                message = "No resume attached to this application."
            });
        }

        var (fileStream, contentType, fileName) = resumeResult.Value;

        return File(fileStream, contentType, fileName);
    }

    // ==========================================
    // UPDATE APPLICATION STATUS
    // ==========================================

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateApplicationStatusDto dto)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result =
            await _applicationService.UpdateStatusAsync(
                id,
                dto.Status,
                userId);

        if (!result.Success)
        {
            if (result.Message == "Application not found.")
            {
                return NotFound(new
                {
                    message = result.Message
                });
            }

            if (result.Message.StartsWith("Invalid status"))
            {
                return BadRequest(new
                {
                    message = result.Message
                });
            }

            return Forbid();
        }

        return Ok(new
        {
            message = result.Message
        });
    }

    // ==========================================
    // DELETE APPLICATION
    // ==========================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result =
            await _applicationService.DeleteAsync(
                id,
                userId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
    }
}