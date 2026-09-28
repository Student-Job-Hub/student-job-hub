namespace StudentJobHub.Api.DTOs.Bookings;

public class ServiceBookingResponseDto
{
    public int Id { get; set; }

    public int ServiceId { get; set; }

    public string ServiceTitle { get; set; } = string.Empty;

    public string ServiceCategory { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public string? ClientUniversity { get; set; }

    public string ProviderId { get; set; } = string.Empty;

    public string ProviderName { get; set; } = string.Empty;

    public string ProviderEmail { get; set; } = string.Empty;

    public DateTime RequestedDate { get; set; }

    public string LocationOrDelivery { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public decimal ProposedPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ProviderResponse { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
