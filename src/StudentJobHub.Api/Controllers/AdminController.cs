using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentJobHub.Api.DTOs.Admin;
using StudentJobHub.Api.Services;

namespace StudentJobHub.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview()
    {
        return Ok(await _adminService.GetOverviewAsync());
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers([FromQuery] string? search)
    {
        return Ok(await _adminService.GetUsersAsync(search));
    }

    [HttpPatch("users/{userId}/suspension")]
    public async Task<IActionResult> SetUserSuspension(
        string userId,
        [FromBody] SetUserSuspensionDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == currentUserId && dto.IsSuspended)
        {
            return BadRequest(new { message = "You cannot suspend your own account." });
        }

        var updated = await _adminService.SetUserSuspensionAsync(userId, dto.IsSuspended);
        return updated ? NoContent() : NotFound();
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<List<AdminJobDto>>> GetJobs()
    {
        return Ok(await _adminService.GetJobsAsync());
    }

    [HttpPatch("jobs/{jobId:int}/close")]
    public async Task<IActionResult> CloseJob(int jobId)
    {
        return await _adminService.CloseJobAsync(jobId) ? NoContent() : NotFound();
    }

    [HttpGet("services")]
    public async Task<ActionResult<List<AdminServiceDto>>> GetServices()
    {
        return Ok(await _adminService.GetServicesAsync());
    }

    [HttpDelete("services/{serviceId:int}")]
    public async Task<IActionResult> DeleteService(int serviceId)
    {
        return await _adminService.DeleteServiceAsync(serviceId) ? NoContent() : NotFound();
    }
}