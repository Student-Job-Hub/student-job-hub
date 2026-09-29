using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentJobHub.Api.Services;

namespace StudentJobHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookmarksController : ControllerBase
{
    private readonly JobBookmarkService _bookmarkService;

    public BookmarksController(JobBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSavedJobs()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var bookmarks = await _bookmarkService.GetSavedJobsAsync(userId);
        return Ok(bookmarks);
    }

    [HttpGet("ids")]
    public async Task<IActionResult> GetSavedJobIds()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var ids = await _bookmarkService.GetSavedJobIdsAsync(userId);
        return Ok(ids);
    }

    [HttpGet("check/{jobId:int}")]
    public async Task<IActionResult> CheckIsBookmarked(int jobId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var isBookmarked = await _bookmarkService.IsJobBookmarkedAsync(jobId, userId);
        return Ok(new { isBookmarked });
    }

    [HttpPost("{jobId:int}")]
    public async Task<IActionResult> BookmarkJob(int jobId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _bookmarkService.BookmarkJobAsync(jobId, userId);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result.Bookmark);
    }

    [HttpDelete("{jobId:int}")]
    public async Task<IActionResult> RemoveBookmark(int jobId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _bookmarkService.RemoveBookmarkAsync(jobId, userId);
        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
    }
}
