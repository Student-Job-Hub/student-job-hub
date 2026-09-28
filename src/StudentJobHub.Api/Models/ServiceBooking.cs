namespace StudentJobHub.Api.Models;

public class ServiceBooking
{
    public int Id { get; set; }

    public int ServiceId { get; set; }

    public Service? Service { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public ApplicationUser? Client { get; set; }

    public string ProviderId { get; set; } = string.Empty;

    public ApplicationUser? Provider { get; set; }

    public DateTime RequestedDate { get; set; }

    public string LocationOrDelivery { get; set; } = "On Campus / Remote";

    public string Notes { get; set; } = string.Empty;

    public decimal ProposedPrice { get; set; }

    public string Status { get; set; } = "Pending";

    public string? ProviderResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
