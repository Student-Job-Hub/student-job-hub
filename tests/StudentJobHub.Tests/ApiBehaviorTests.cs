using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Applications;
using StudentJobHub.Api.DTOs.Auth;
using StudentJobHub.Api.DTOs.Jobs;
using StudentJobHub.Api.DTOs.Reviews;
using StudentJobHub.Api.DTOs.Services;
using StudentJobHub.Api.Hubs;
using StudentJobHub.Api.Models;
using StudentJobHub.Api.Services;
using Xunit;

namespace StudentJobHub.Tests;

public class ApiBehaviorTests
{
    private static async Task<(
        ApplicationDbContext Context,
        UserManager<ApplicationUser> UserManager,
        AuthService AuthService,
        JobService JobService,
        JobApplicationService ApplicationService,
        ServiceService ServiceService,
        NotificationService NotificationService,
        ReviewService ReviewService)> CreateServicesAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        var authService = new AuthService(userManager, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "StudentJobHub_Test_Key_1234567890_ABCDEFGH1234567890",
            ["Jwt:Issuer"] = "StudentJobHub",
            ["Jwt:Audience"] = "StudentJobHubAudience"
        }).Build());

        var jobService = new JobService(context);
        var notificationService = new NotificationService(context);

        var testHubContext = new TestHubContext();
        var applicationService = new JobApplicationService(context, notificationService, testHubContext);
        var serviceService = new ServiceService(context);
        var reviewService = new ReviewService(context);

        await EnsureRolesAsync(roleManager);

        return (context, userManager, authService, jobService, applicationService, serviceService, notificationService, reviewService);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in new[] { "Student", "Business", "Lecturer" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserAndGeneratesToken()
    {
        var (_, _, authService, _, _, _, _, _) = await CreateServicesAsync();

        var result = await authService.RegisterAsync(new RegisterDto
        {
            FullName = "Student User",
            Email = "student@test.com",
            Password = "Password123!",
            Role = "Student",
            University = "Test University"
        });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_Fails()
    {
        var (_, _, authService, _, _, _, _, _) = await CreateServicesAsync();

        await authService.RegisterAsync(new RegisterDto
        {
            FullName = "Student User",
            Email = "student@test.com",
            Password = "Password123!",
            Role = "Student"
        });

        var result = await authService.RegisterAsync(new RegisterDto
        {
            FullName = "Another User",
            Email = "student@test.com",
            Password = "Password123!",
            Role = "Student"
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AdminBootstrap_CreatesAdminThatCanSignIn_AndIsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole("Admin"));

        var bootstrap = new AdminBootstrapService(userManager, roleManager);
        await bootstrap.EnsureAdminAsync("admin@test.com", "AdminPass123!");
        await bootstrap.EnsureAdminAsync("admin@test.com", "AdminPass123!");

        var user = await userManager.FindByEmailAsync("admin@test.com");
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user!, "Admin"));
        Assert.Equal(1, await userManager.Users.CountAsync());

        var authService = new AuthService(userManager, new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "StudentJobHub_Test_Key_1234567890_ABCDEFGH1234567890",
                ["Jwt:Issuer"] = "StudentJobHub",
                ["Jwt:Audience"] = "StudentJobHubAudience"
            })
            .Build());
        var login = await authService.LoginAsync(new LoginDto
        {
            Email = "admin@test.com",
            Password = "AdminPass123!"
        });

        Assert.True(login.Success);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var (_, _, authService, _, _, _, _, _) = await CreateServicesAsync();

        await authService.RegisterAsync(new RegisterDto
        {
            FullName = "Student User",
            Email = "student@test.com",
            Password = "Password123!",
            Role = "Student"
        });

        var result = await authService.LoginAsync(new LoginDto
        {
            Email = "student@test.com",
            Password = "Password123!"
        });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task SuspendedUser_CannotLoginOrRefreshUntilReactivated()
    {
        var (context, userManager, authService, _, _, _, _, _) = await CreateServicesAsync();
        var registration = await authService.RegisterAsync(new RegisterDto
        {
            FullName = "Member User",
            Email = "suspended@test.com",
            Password = "Password123!",
            Role = "Student"
        });
        var user = await userManager.FindByEmailAsync("suspended@test.com");
        Assert.NotNull(user);

        var adminService = new AdminService(context, userManager);
        Assert.True(await adminService.SetUserSuspensionAsync(user!.Id, true));

        var login = await authService.LoginAsync(new LoginDto
        {
            Email = "suspended@test.com",
            Password = "Password123!"
        });
        var refresh = await authService.RefreshAsync(registration.RefreshToken!);

        Assert.False(login.Success);
        Assert.False(refresh.Success);

        Assert.True(await adminService.SetUserSuspensionAsync(user.Id, false));
        var reactivatedLogin = await authService.LoginAsync(new LoginDto
        {
            Email = "suspended@test.com",
            Password = "Password123!"
        });
        Assert.True(reactivatedLogin.Success);
    }

    [Fact]
    public async Task AdminModeration_ClosesJobsAndRemovesServices()
    {
        var (context, userManager, _, _, _, _, _, _) = await CreateServicesAsync();
        var owner = new ApplicationUser
        {
            Id = "moderation-owner",
            UserName = "owner@test.com",
            Email = "owner@test.com",
            FullName = "Listing Owner"
        };
        context.Users.Add(owner);
        var job = new Job
        {
            Title = "Moderated job",
            PostedById = owner.Id
        };
        var service = new StudentJobHub.Api.Models.Service
        {
            Title = "Moderated service",
            ProviderId = owner.Id
        };
        context.Jobs.Add(job);
        context.Services.Add(service);
        await context.SaveChangesAsync();

        var adminService = new AdminService(context, userManager);
        Assert.True(await adminService.CloseJobAsync(job.Id));
        Assert.True(await adminService.DeleteServiceAsync(service.Id));
        Assert.False((await context.Jobs.FindAsync(job.Id))!.IsOpen);
        Assert.Null(await context.Services.FindAsync(service.Id));
    }

    [Fact]
    public async Task RefreshAsync_RotatesTokenAndRejectsPreviousToken()
    {
        var (_, _, authService, _, _, _, _, _) = await CreateServicesAsync();
        var registration = await authService.RegisterAsync(new RegisterDto
        {
            FullName = "Student User",
            Email = "refresh@test.com",
            Password = "Password123!",
            Role = "Student"
        });

        var refreshed = await authService.RefreshAsync(registration.RefreshToken!);
        var replayed = await authService.RefreshAsync(registration.RefreshToken!);

        Assert.True(refreshed.Success);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.Token));
        Assert.NotEqual(registration.RefreshToken, refreshed.RefreshToken);
        Assert.False(replayed.Success);
    }

    [Fact]
    public async Task JobService_CreateAndGetById_Succeeds()
    {
        var (context, _, _, jobService, _, _, _, _) = await CreateServicesAsync();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "poster@test.com",
            Email = "poster@test.com",
            FullName = "Poster User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var created = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Test Job",
            Description = "Description",
            Requirements = "Requirements",
            Budget = 100,
            Deadline = DateTime.UtcNow.AddDays(7)
        }, user.Id);

        var retrieved = await jobService.GetByIdAsync(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Test Job", retrieved!.Title);
        Assert.Equal(user.Id, created.PostedById);
    }

    [Fact]
    public async Task JobService_UpdateAndDeletePermissions_Enforced()
    {
        var (context, _, _, jobService, _, _, _, _) = await CreateServicesAsync();

        var owner = new ApplicationUser { Id = "owner1", FullName = "Owner", Email = "owner@test.com" };
        var intruder = new ApplicationUser { Id = "intruder", FullName = "Intruder", Email = "intruder@test.com" };
        context.Users.AddRange(owner, intruder);
        await context.SaveChangesAsync();

        var job = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Original Job",
            Description = "Desc",
            Requirements = "Req",
            Budget = 200,
            Deadline = DateTime.UtcNow.AddDays(5)
        }, owner.Id);

        var updateResult = await jobService.UpdateAsync(job.Id, new UpdateJobDto
        {
            Title = "Updated Job",
            Description = "Desc",
            Requirements = "Req",
            Budget = 250,
            Deadline = DateTime.UtcNow.AddDays(5)
        }, intruder.Id);

        Assert.False(updateResult);

        var deleteResult = await jobService.DeleteAsync(job.Id, intruder.Id);
        Assert.False(deleteResult);

        var ownerDeleteResult = await jobService.DeleteAsync(job.Id, owner.Id);
        Assert.True(ownerDeleteResult);
    }

    [Fact]
    public async Task JobApplicationService_ApplicationWorkflow_Succeeds()
    {
        var (context, _, _, jobService, appService, _, _, _) = await CreateServicesAsync();

        var owner = new ApplicationUser { Id = "owner", FullName = "Owner", Email = "owner@test.com" };
        var applicant = new ApplicationUser { Id = "applicant", FullName = "Applicant", Email = "applicant@test.com" };
        context.Users.AddRange(owner, applicant);
        await context.SaveChangesAsync();

        var job = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Web Dev Job",
            Description = "Desc",
            Requirements = "C#",
            Budget = 500,
            Deadline = DateTime.UtcNow.AddDays(10)
        }, owner.Id);

        // Self apply check
        var selfApply = await appService.CreateAsync(job.Id, new CreateApplicationDto { Message = "Me" }, owner.Id);
        Assert.False(selfApply.Success);

        // Valid application
        var apply = await appService.CreateAsync(job.Id, new CreateApplicationDto { Message = "I can build this!" }, applicant.Id);
        Assert.True(apply.Success);
        Assert.NotNull(apply.Application);

        // Duplicate apply check
        var dupApply = await appService.CreateAsync(job.Id, new CreateApplicationDto { Message = "Again" }, applicant.Id);
        Assert.False(dupApply.Success);

        // Owner status update
        var updateStatus = await appService.UpdateStatusAsync(apply.Application!.Id, "Accepted", owner.Id);
        Assert.True(updateStatus.Success);

        var updatedApp = await appService.GetByIdAsync(apply.Application.Id);
        Assert.NotNull(updatedApp);
        Assert.Equal("Accepted", updatedApp!.Status);
    }

    [Fact]
    public async Task ServiceService_CreateAndGet_Succeeds()
    {
        var (context, _, _, _, _, serviceService, _, _) = await CreateServicesAsync();

        var provider = new ApplicationUser { Id = "provider1", FullName = "Provider", Email = "provider@test.com" };
        context.Users.Add(provider);
        await context.SaveChangesAsync();

        var created = await serviceService.CreateAsync(new CreateServiceDto
        {
            Title = "Tutoring Service",
            Category = "Education",
            Description = "Math & CS tutoring",
            Price = 30
        }, provider.Id);

        Assert.NotNull(created);
        Assert.Equal("Tutoring Service", created.Title);

        var all = await serviceService.GetAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task ReviewService_PreventSelfReview_Succeeds()
    {
        var (context, _, _, _, _, _, _, reviewService) = await CreateServicesAsync();

        var user = new ApplicationUser { Id = "u1", FullName = "User 1", Email = "u1@test.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await reviewService.CreateAsync(new CreateReviewDto
        {
            RevieweeId = user.Id,
            Rating = 5,
            Comment = "Self review"
        }, user.Id);

        Assert.False(result.Success);
        Assert.Equal("You cannot review yourself.", result.Message);
    }

    [Fact]
    public async Task ReviewService_CreateAndRetrieve_Succeeds()
    {
        var (context, _, _, _, _, _, _, reviewService) = await CreateServicesAsync();

        var reviewer = new ApplicationUser { Id = "rev1", FullName = "Reviewer Person", Email = "rev1@test.com" };
        var reviewee = new ApplicationUser { Id = "target1", FullName = "Service Provider", Email = "target1@test.com" };
        context.Users.AddRange(reviewer, reviewee);
        await context.SaveChangesAsync();

        // 1. Create valid review
        var result = await reviewService.CreateAsync(new CreateReviewDto
        {
            RevieweeId = reviewee.Id,
            Rating = 5,
            Comment = "Excellent web development service, highly recommended!"
        }, reviewer.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Review);
        Assert.Equal(5, result.Review!.Rating);
        Assert.Equal("Reviewer Person", result.Review.ReviewerName);

        // 2. Prevent duplicate review
        var dupResult = await reviewService.CreateAsync(new CreateReviewDto
        {
            RevieweeId = reviewee.Id,
            Rating = 4,
            Comment = "Duplicate review attempt"
        }, reviewer.Id);

        Assert.False(dupResult.Success);
        Assert.Equal("You have already reviewed this user.", dupResult.Message);

        // 3. Get user reviews
        var userReviews = await reviewService.GetUserReviewsAsync(reviewee.Id);
        Assert.Single(userReviews);
        Assert.Equal("Excellent web development service, highly recommended!", userReviews[0].Comment);

        // 4. Delete review authorization check
        var nonAuthorDelete = await reviewService.DeleteAsync(result.Review.Id, "intruder_id");
        Assert.False(nonAuthorDelete);

        var authorDelete = await reviewService.DeleteAsync(result.Review.Id, reviewer.Id);
        Assert.True(authorDelete);

        var afterDeleteReviews = await reviewService.GetUserReviewsAsync(reviewee.Id);
        Assert.Empty(afterDeleteReviews);
    }
}

// ============================================================
// SIGNALR TEST STUBS
// ============================================================

public class TestHubContext : IHubContext<NotificationHub>
{
    public IHubClients Clients => new TestHubClients();
    public IGroupManager Groups => new TestGroupManager();
}

public class TestGroupManager : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class TestHubClients : IHubClients
{
    public IClientProxy All => new TestClientProxy();
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new TestClientProxy();
    public IClientProxy Client(string connectionId) => new TestClientProxy();
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new TestClientProxy();
    public IClientProxy Group(string groupName) => new TestClientProxy();
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new TestClientProxy();
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => new TestClientProxy();
    public IClientProxy User(string userId) => new TestClientProxy();
    public IClientProxy Users(IReadOnlyList<string> userIds) => new TestClientProxy();
}

public class TestClientProxy : IClientProxy
{
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
