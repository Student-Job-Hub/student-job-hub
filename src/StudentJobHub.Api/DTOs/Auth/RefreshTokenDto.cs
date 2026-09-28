using System.ComponentModel.DataAnnotations;

namespace StudentJobHub.Api.DTOs.Auth;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}