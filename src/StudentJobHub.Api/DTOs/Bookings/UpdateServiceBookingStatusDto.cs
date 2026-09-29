using System.ComponentModel.DataAnnotations;

namespace StudentJobHub.Api.DTOs.Bookings;

public class UpdateServiceBookingStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ProviderResponse { get; set; }
}
