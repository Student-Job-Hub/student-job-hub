using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Bookmarks;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Services;

public class JobBookmarkService
{
    private readonly ApplicationDbContext _context;

    public JobBookmarkService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobBookmarkDto>> GetSavedJobsAsync(string userId)
    {
        return await _context.JobBookmarks
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
    }

    public async Task<List<int>> GetSavedJobIdsAsync(string userId)
    {
        return await _context.JobBookmarks
            .Where(b => b.UserId == userId)
            .Select(b => b.JobId)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, JobBookmarkDto? Bookmark)> BookmarkJobAsync(int jobId, string userId)
    {
        var job = await _context.Jobs
            .Include(j => j.PostedBy)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            return (false, "Job not found.", null);
        }

        var existing = await _context.JobBookmarks
            .FirstOrDefaultAsync(b => b.JobId == jobId && b.UserId == userId);

        if (existing != null)
        {
            return (false, "Job is already bookmarked.", null);
        }

        var bookmark = new JobBookmark
        {
            JobId = jobId,
            UserId = userId,
            SavedAt = DateTime.UtcNow
        };

        _context.JobBookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        var dto = new JobBookmarkDto
        {
            Id = bookmark.Id,
            JobId = job.Id,
            Title = job.Title,
            Description = job.Description,
            Category = job.Category,
            Requirements = job.Requirements,
            Budget = job.Budget,
            Deadline = job.Deadline,
            PostedById = job.PostedById,
            PostedByName = job.PostedBy != null ? job.PostedBy.FullName : string.Empty,
            IsOpen = job.IsOpen,
            JobCreatedAt = job.CreatedAt,
            SavedAt = bookmark.SavedAt
        };

        return (true, "Job saved to bookmarks.", dto);
    }

    public async Task<(bool Success, string Message)> RemoveBookmarkAsync(int jobId, string userId)
    {
        var bookmark = await _context.JobBookmarks
            .FirstOrDefaultAsync(b => b.JobId == jobId && b.UserId == userId);

        if (bookmark == null)
        {
            return (false, "Bookmark not found.");
        }

        _context.JobBookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        return (true, "Bookmark removed successfully.");
    }

    public async Task<bool> IsJobBookmarkedAsync(int jobId, string userId)
    {
        return await _context.JobBookmarks
            .AnyAsync(b => b.JobId == jobId && b.UserId == userId);
    }
}
