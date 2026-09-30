using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Jobs;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Services;

public class JobService
{
    private readonly ApplicationDbContext _context;

    public JobService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobResponseDto> CreateAsync(
        CreateJobDto dto,
        string postedById)
    {
        var job = new Job
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Requirements = dto.Requirements,
            Budget = dto.Budget,
            Deadline = dto.Deadline,
            PostedById = postedById,
            IsOpen = true
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return await GetResponseAsync(job.Id)
            ?? throw new InvalidOperationException(
                "Failed to retrieve the created job.");
    }

    // GET ALL JOBS
    public async Task<List<JobResponseDto>> GetAllAsync()
    {
        return await _context.Jobs
            .Include(j => j.PostedBy)
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
                PostedByName = j.PostedBy != null
                    ? j.PostedBy.FullName
                    : string.Empty,
                IsOpen = j.IsOpen,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync();
    }

    // GET JOBS BELONGING TO CURRENT USER
    public async Task<List<JobResponseDto>> GetMyJobsAsync(
        string postedById)
    {
        return await _context.Jobs
            .Include(j => j.PostedBy)
            .Where(j => j.PostedById == postedById)
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
                PostedByName = j.PostedBy != null
                    ? j.PostedBy.FullName
                    : string.Empty,
                IsOpen = j.IsOpen,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<JobResponseDto?> GetByIdAsync(int id)
    {
        return await GetResponseAsync(id);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateJobDto dto,
        string postedById)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j =>
                j.Id == id &&
                j.PostedById == postedById);

        if (job == null)
        {
            return false;
        }

        job.Title = dto.Title;
        job.Description = dto.Description;
        job.Category = dto.Category;
        job.Requirements = dto.Requirements;
        job.Budget = dto.Budget;
        job.Deadline = dto.Deadline;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        string postedById)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j =>
                j.Id == id &&
                j.PostedById == postedById);

        if (job == null)
        {
            return false;
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(
            postedById,
            "CREATE",
            "Job",
            job.Id.ToString(),
            $"Created job '{job.Title}'.");
        return true;
    }

    public async Task<bool> CloseAsync(
        int id,
        string postedById)
    {
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j =>
                j.Id == id &&
                j.PostedById == postedById);

        if (job == null)
        {
            return false;
        }

        job.IsOpen = false;

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(
            postedById,
            "CREATE",
            "Job",
            job.Id.ToString(),
            $"Created job '{job.Title}'.");
        return true;
    }

    private async Task<JobResponseDto?> GetResponseAsync(int id)
    {
        return await _context.Jobs
            .Include(j => j.PostedBy)
            .Where(j => j.Id == id)
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
                PostedByName = j.PostedBy != null
                    ? j.PostedBy.FullName
                    : string.Empty,
                IsOpen = j.IsOpen,
                CreatedAt = j.CreatedAt
            })
            .FirstOrDefaultAsync();
    }
}
