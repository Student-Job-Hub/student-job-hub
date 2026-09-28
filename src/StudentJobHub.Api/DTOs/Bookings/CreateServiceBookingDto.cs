using System.ComponentModel.DataAnnotations;

namespace StudentJobHub.Api.DTOs.Bookings;

public class CreateServiceBookingDto
{
    [Required]
    public int ServiceId { get; set; }

    [Required]
    public DateTime RequestedDate { get; set; }

    [MaxLength(200)]
    public string LocationOrDelivery { get; set; } = "On Campus / Remote";

    [Required]
    [MinLength(5, ErrorMessage = "Please provide at least 5 characters detailing your request.")]
    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "Price must be a non-negative amount.")]
    public decimal ProposedPrice { get; set; }
}
