using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StudentJobHub.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public string? Bio { get; set; }

    public string? University { get; set; }

    [MaxLength(64)]
    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}