using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Admin;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Services;

public class AdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<AdminOverviewDto> GetOverviewAsync()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return new AdminOverviewDto
        {
            TotalUsers = await _userManager.Users.CountAsync(),
            SuspendedUsers = await _userManager.Users.CountAsync(user =>
                user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow),
            TotalJobs = await _context.Jobs.CountAsync(),
            OpenJobs = await _context.Jobs.CountAsync(job => job.IsOpen),
            TotalServices = await _context.Services.CountAsync(),
            TotalApplications = await _context.JobApplications.CountAsync(),
            NewUsersThisMonth = await _userManager.Users.CountAsync(user => user.CreatedAt >= monthStart)
        };
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(string? search)
    {
        var users = await _userManager.Users
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            users = users.Where(user =>
                    user.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (user.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        var result = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(new AdminUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                University = user.University,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                IsSuspended = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
                CreatedAt = user.CreatedAt
            });
        }

        return result;
    }

    public async Task<bool> SetUserSuspensionAsync(string userId, bool isSuspended)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var enableLockoutResult = await _userManager.SetLockoutEnabledAsync(user, true);
        if (!enableLockoutResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", enableLockoutResult.Errors.Select(error => error.Description)));
        }

        DateTimeOffset? lockoutEnd = isSuspended ? DateTimeOffset.MaxValue : null;
        var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        if (!lockoutResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", lockoutResult.Errors.Select(error => error.Description)));
        }

        if (isSuspended)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(error => error.Description)));
            }
        }

        return true;
    }

    public async Task<List<AdminJobDto>> GetJobsAsync()
    {
        return await _context.Jobs
            .OrderByDescending(job => job.CreatedAt)
            .Select(job => new AdminJobDto
            {
                Id = job.Id,
                Title = job.Title,
                PostedByName = job.PostedBy == null ? string.Empty : job.PostedBy.FullName,
                PostedByEmail = job.PostedBy == null ? string.Empty : job.PostedBy.Email ?? string.Empty,
                IsOpen = job.IsOpen,
                CreatedAt = job.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> CloseJobAsync(int jobId)
    {
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return false;
        }

        job.IsOpen = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AdminServiceDto>> GetServicesAsync()
    {
        return await _context.Services
            .OrderByDescending(service => service.CreatedAt)
            .Select(service => new AdminServiceDto
            {
                Id = service.Id,
                Title = service.Title,
                ProviderName = service.Provider == null ? string.Empty : service.Provider.FullName,
                ProviderEmail = service.Provider == null ? string.Empty : service.Provider.Email ?? string.Empty,
                Price = service.Price,
                CreatedAt = service.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteServiceAsync(int serviceId)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null)
        {
            return false;
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return true;
    }
}
