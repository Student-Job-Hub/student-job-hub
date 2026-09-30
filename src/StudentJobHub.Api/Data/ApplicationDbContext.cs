using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Service> Services => Set<Service>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
    
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<JobBookmark> JobBookmarks => Set<JobBookmark>();

    public DbSet<ServiceBooking> ServiceBookings => Set<ServiceBooking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ==========================================
        // JOB
        // ==========================================

        builder.Entity<Job>()
            .HasOne(j => j.PostedBy)
            .WithMany()
            .HasForeignKey(j => j.PostedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Job>()
            .Property(j => j.Budget)
            .HasPrecision(18, 2);

        // ==========================================
        // SERVICE
        // ==========================================

        builder.Entity<Service>()
            .HasOne(s => s.Provider)
            .WithMany()
            .HasForeignKey(s => s.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(18, 2);

        // ==========================================
        // JOB APPLICATION
        // ==========================================

        builder.Entity<JobApplication>()
            .HasOne(a => a.Job)
            .WithMany()
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<JobApplication>()
            .HasOne(a => a.Applicant)
            .WithMany()
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // REVIEW
        // ==========================================

        builder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(r => r.Reviewee)
            .WithMany()
            .HasForeignKey(r => r.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // NOTIFICATION
        // ==========================================

        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DirectMessage>()
            .HasOne(message => message.Application)
            .WithMany()
            .HasForeignKey(message => message.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DirectMessage>()
            .HasOne(message => message.Sender)
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

                    // ==========================================
        // AUDIT LOG
        // ==========================================
        
        builder.Entity<AuditLog>()
            .HasOne(log => log.User)
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // ==========================================
        // JOB BOOKMARK
        // ==========================================

        builder.Entity<JobBookmark>()
            .HasIndex(b => new { b.UserId, b.JobId })
            .IsUnique();

        builder.Entity<JobBookmark>()
            .HasOne(b => b.Job)
            .WithMany()
            .HasForeignKey(b => b.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JobBookmark>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // SERVICE BOOKING
        // ==========================================

        builder.Entity<ServiceBooking>()
            .HasOne(b => b.Service)
            .WithMany()
            .HasForeignKey(b => b.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceBooking>()
            .HasOne(b => b.Client)
            .WithMany()
            .HasForeignKey(b => b.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceBooking>()
            .HasOne(b => b.Provider)
            .WithMany()
            .HasForeignKey(b => b.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceBooking>()
            .Property(b => b.ProposedPrice)
            .HasPrecision(18, 2);
    }
}
