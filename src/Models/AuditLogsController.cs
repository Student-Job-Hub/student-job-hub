using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentJobHub.Api.Services;

namespace StudentJobHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly AuditLogService _auditLogService;

    public AuditLogsController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 200)
    {
        var logs = await _auditLogService.GetRecentAsync(take);

        return Ok(logs.Select(log => new
        {
            log.Id,
            log.UserId,
            UserName = log.User?.FullName,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.Details,
            log.CreatedAt
        }));
    }
}
