using System.ComponentModel.DataAnnotations;

namespace StudentJobHub.Client.Models;

public class UpdateBookingStatusModel
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ProviderResponse { get; set; }
}
