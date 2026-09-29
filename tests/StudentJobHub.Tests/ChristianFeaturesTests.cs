using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Bookings;
using StudentJobHub.Api.DTOs.Jobs;
using StudentJobHub.Api.DTOs.Services;
using StudentJobHub.Api.Models;
using StudentJobHub.Api.Services;
using Xunit;

namespace StudentJobHub.Tests;

public class ChristianFeaturesTests
{
    private static async Task<(
        ApplicationDbContext Context,
        JobBookmarkService BookmarkService,
        ServiceBookingService BookingService,
        ExportService ExportService,
        JobService JobService,
        ServiceService ServiceService,
        ApplicationUser User1,
        ApplicationUser User2)> CreateTestSetupAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName));

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<ApplicationDbContext>();

        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user1@campus.edu",
            Email = "user1@campus.edu",
            FullName = "Christian User",
            University = "University of Ghana"
        };

        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user2@campus.edu",
            Email = "user2@campus.edu",
            FullName = "Student Employer",
            University = "University of Ghana"
        };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var notificationService = new NotificationService(context);
        var testHub = new TestHubContext();

        var bookmarkService = new JobBookmarkService(context);
        var bookingService = new ServiceBookingService(context, notificationService, testHub);
        var exportService = new ExportService(context);
        var jobService = new JobService(context);
        var serviceService = new ServiceService(context);

        return (context, bookmarkService, bookingService, exportService, jobService, serviceService, user1, user2);
    }

    // =========================================================================
    // FEATURE #19: JOB BOOKMARKS / SAVED JOBS TESTS
    // =========================================================================

    [Fact]
    public async Task BookmarkJobAsync_Succeeds_AndCanRetrieve()
    {
        var (context, bookmarkService, _, _, jobService, _, user1, user2) = await CreateTestSetupAsync();

        var job = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Campus Lab Assistant",
            Category = "IT Support",
            Description = "Assist in computer lab",
            Requirements = "Basic hardware knowledge",
            Budget = 250,
            Deadline = DateTime.UtcNow.AddDays(14)
        }, user2.Id);

        // Bookmark job
        var result = await bookmarkService.BookmarkJobAsync(job.Id, user1.Id);
        Assert.True(result.Success);
        Assert.NotNull(result.Bookmark);
        Assert.Equal(job.Id, result.Bookmark!.JobId);
        Assert.Equal("Campus Lab Assistant", result.Bookmark.Title);

        // Check is bookmarked
        var isBookmarked = await bookmarkService.IsJobBookmarkedAsync(job.Id, user1.Id);
        Assert.True(isBookmarked);

        // Get saved jobs list
        var savedJobs = await bookmarkService.GetSavedJobsAsync(user1.Id);
        Assert.Single(savedJobs);
        Assert.Equal(job.Id, savedJobs[0].JobId);

        // Get saved job IDs
        var ids = await bookmarkService.GetSavedJobIdsAsync(user1.Id);
        Assert.Contains(job.Id, ids);
    }

    [Fact]
    public async Task BookmarkJobAsync_Fails_WhenDuplicate()
    {
        var (context, bookmarkService, _, _, jobService, _, user1, user2) = await CreateTestSetupAsync();

        var job = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Library Organizer",
            Category = "Administration",
            Description = "Sort library books",
            Budget = 100,
            Deadline = DateTime.UtcNow.AddDays(7)
        }, user2.Id);

        // First bookmark
        var first = await bookmarkService.BookmarkJobAsync(job.Id, user1.Id);
        Assert.True(first.Success);

        // Second bookmark (duplicate)
        var second = await bookmarkService.BookmarkJobAsync(job.Id, user1.Id);
        Assert.False(second.Success);
        Assert.Equal("Job is already bookmarked.", second.Message);
    }

    [Fact]
    public async Task RemoveBookmarkAsync_Succeeds()
    {
        var (context, bookmarkService, _, _, jobService, _, user1, user2) = await CreateTestSetupAsync();

        var job = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Math Tutor Needed",
            Category = "Tutoring",
            Description = "Linear Algebra tutoring",
            Budget = 150,
            Deadline = DateTime.UtcNow.AddDays(5)
        }, user2.Id);

        await bookmarkService.BookmarkJobAsync(job.Id, user1.Id);
        Assert.True(await bookmarkService.IsJobBookmarkedAsync(job.Id, user1.Id));

        var removeResult = await bookmarkService.RemoveBookmarkAsync(job.Id, user1.Id);
        Assert.True(removeResult.Success);

        Assert.False(await bookmarkService.IsJobBookmarkedAsync(job.Id, user1.Id));
        var saved = await bookmarkService.GetSavedJobsAsync(user1.Id);
        Assert.Empty(saved);
    }

    // =========================================================================
    // FEATURE #24: SERVICE REQUEST / BOOKING SYSTEM TESTS
    // =========================================================================

    [Fact]
    public async Task CreateBookingAsync_Succeeds_AndNotifiesProvider()
    {
        var (context, _, bookingService, _, _, serviceService, user1, user2) = await CreateTestSetupAsync();

        // User2 creates a service
        var service = await serviceService.CreateAsync(new CreateServiceDto
        {
            Title = "Web Development & UI Design",
            Category = "Software",
            Description = "Full stack web apps with ASP.NET & Blazor",
            Price = 350
        }, user2.Id);

        // User1 books the service
        var bookingResult = await bookingService.CreateBookingAsync(new CreateServiceBookingDto
        {
            ServiceId = service.Id,
            RequestedDate = DateTime.UtcNow.AddDays(3),
            LocationOrDelivery = "Remote / Online",
            Notes = "Need a portfolio landing page developed",
            ProposedPrice = 350
        }, user1.Id);

        Assert.True(bookingResult.Success);
        Assert.NotNull(bookingResult.Booking);
        Assert.Equal("Pending", bookingResult.Booking!.Status);
        Assert.Equal(user1.Id, bookingResult.Booking.ClientId);
        Assert.Equal(user2.Id, bookingResult.Booking.ProviderId);
        Assert.Equal(350, bookingResult.Booking.ProposedPrice);

        // Verify Client can view it in My Requests
        var clientRequests = await bookingService.GetClientBookingsAsync(user1.Id);
        Assert.Single(clientRequests);
        Assert.Equal("Web Development & UI Design", clientRequests[0].ServiceTitle);

        // Verify Provider can view it in Received Requests
        var providerReceived = await bookingService.GetProviderBookingsAsync(user2.Id);
        Assert.Single(providerReceived);
        Assert.Equal("Christian User", providerReceived[0].ClientName);
    }

    [Fact]
    public async Task CreateBookingAsync_CannotBookOwnService()
    {
        var (context, _, bookingService, _, _, serviceService, user1, _) = await CreateTestSetupAsync();

        var service = await serviceService.CreateAsync(new CreateServiceDto
        {
            Title = "Graphic Design",
            Category = "Design",
            Description = "Flyers and logos",
            Price = 80
        }, user1.Id);

        // User1 tries to book own service
        var result = await bookingService.CreateBookingAsync(new CreateServiceBookingDto
        {
            ServiceId = service.Id,
            RequestedDate = DateTime.UtcNow.AddDays(2),
            Notes = "Booking myself"
        }, user1.Id);

        Assert.False(result.Success);
        Assert.Equal("You cannot book your own service.", result.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_ProviderAcceptsAndCompletes_ClientCancels()
    {
        var (context, _, bookingService, _, _, serviceService, user1, user2) = await CreateTestSetupAsync();

        var service = await serviceService.CreateAsync(new CreateServiceDto
        {
            Title = "Photography Service",
            Category = "Media",
            Description = "Campus events photography",
            Price = 200
        }, user2.Id);

        var created = await bookingService.CreateBookingAsync(new CreateServiceBookingDto
        {
            ServiceId = service.Id,
            RequestedDate = DateTime.UtcNow.AddDays(4),
            Notes = "Department dinner photos"
        }, user1.Id);

        var bookingId = created.Booking!.Id;

        // Provider accepts booking with a response
        var acceptResult = await bookingService.UpdateStatusAsync(bookingId, new UpdateServiceBookingStatusDto
        {
            Status = "Accepted",
            ProviderResponse = "I have confirmed the date and will bring my equipment."
        }, user2.Id);

        Assert.True(acceptResult.Success);

        var acceptedBooking = await bookingService.GetBookingByIdAsync(bookingId, user1.Id);
        Assert.NotNull(acceptedBooking);
        Assert.Equal("Accepted", acceptedBooking!.Status);
        Assert.Equal("I have confirmed the date and will bring my equipment.", acceptedBooking.ProviderResponse);

        // Provider marks completed
        var completeResult = await bookingService.UpdateStatusAsync(bookingId, new UpdateServiceBookingStatusDto
        {
            Status = "Completed"
        }, user2.Id);

        Assert.True(completeResult.Success);
        var completedBooking = await bookingService.GetBookingByIdAsync(bookingId, user1.Id);
        Assert.Equal("Completed", completedBooking!.Status);
    }

    // =========================================================================
    // FEATURE #29: DATA EXPORT TESTS
    // =========================================================================

    [Fact]
    public async Task ExportApplicationsCsv_GeneratesValidCsvBytes()
    {
        var (context, _, _, exportService, jobService, _, user1, user2) = await CreateTestSetupAsync();

        var job = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Research Assistant",
            Category = "Academic",
            Description = "Assist in literature review",
            Budget = 300,
            Deadline = DateTime.UtcNow.AddDays(10)
        }, user2.Id);

        var app = new JobApplication
        {
            JobId = job.Id,
            ApplicantId = user1.Id,
            Message = "Experienced in academic research",
            Status = "Pending",
            AppliedAt = DateTime.UtcNow
        };
        context.JobApplications.Add(app);
        await context.SaveChangesAsync();

        var bytes = await exportService.ExportApplicationsCsvAsync(user1.Id);
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        var csvText = Encoding.UTF8.GetString(bytes);
        Assert.Contains("Job Title", csvText);
        Assert.Contains("Research Assistant", csvText);
        Assert.Contains("Experienced in academic research", csvText);
    }

    [Fact]
    public async Task ExportJobsCsv_GeneratesValidCsvBytes()
    {
        var (context, _, _, exportService, jobService, _, _, user2) = await CreateTestSetupAsync();

        await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Event Coordinator",
            Category = "Events",
            Description = "Manage hall week event",
            Budget = 500,
            Deadline = DateTime.UtcNow.AddDays(20)
        }, user2.Id);

        var bytes = await exportService.ExportJobsCsvAsync(user2.Id);
        Assert.NotNull(bytes);

        var csvText = Encoding.UTF8.GetString(bytes);
        Assert.Contains("Title,Category,Budget (GHS)", csvText);
        Assert.Contains("Event Coordinator", csvText);
        Assert.Contains("500.00", csvText);
    }

    [Fact]
    public async Task ExportUserDataSummary_AndHtmlReport_GenerateSuccessfully()
    {
        var (context, bookmarkService, bookingService, exportService, jobService, serviceService, user1, user2) = await CreateTestSetupAsync();

        // Add a job, bookmark, and booking
        var job = await jobService.CreateAsync(new CreateJobDto
        {
            Title = "Campus Tutor",
            Category = "Education",
            Description = "Tutoring",
            Budget = 100,
            Deadline = DateTime.UtcNow.AddDays(5)
        }, user2.Id);

        await bookmarkService.BookmarkJobAsync(job.Id, user1.Id);

        var service = await serviceService.CreateAsync(new CreateServiceDto
        {
            Title = "Writing Service",
            Category = "Content",
            Description = "Resume and cover letter writing",
            Price = 50
        }, user2.Id);

        await bookingService.CreateBookingAsync(new CreateServiceBookingDto
        {
            ServiceId = service.Id,
            RequestedDate = DateTime.UtcNow.AddDays(3),
            Notes = "Resume review"
        }, user1.Id);

        // Test Summary JSON DTO
        var summary = await exportService.ExportUserDataSummaryAsync(user1.Id);
        Assert.NotNull(summary);
        Assert.Equal("Christian User", summary.FullName);
        Assert.Single(summary.BookmarkedJobs);
        Assert.Single(summary.ClientBookings);

        // Test HTML printable report
        var html = await exportService.GenerateHtmlReportAsync(user1.Id);
        Assert.NotNull(html);
        Assert.Contains("Student Job Hub — Activity & Career Report", html);
        Assert.Contains("Christian User", html);
        Assert.Contains("Campus Tutor", html);
        Assert.Contains("Writing Service", html);
    }
}
