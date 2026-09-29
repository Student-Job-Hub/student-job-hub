using System.ComponentModel.DataAnnotations;

namespace StudentJobHub.Client.Models;

public class CreateServiceBookingModel
{
    [Required]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Please select your preferred date.")]
    public DateTime RequestedDate { get; set; } = DateTime.Today.AddDays(1);

    [MaxLength(200)]
    public string LocationOrDelivery { get; set; } = "On Campus / Remote";

    [Required(ErrorMessage = "Please describe what service you need and any requirements.")]
    [MinLength(5, ErrorMessage = "Please provide at least 5 characters detailing your request.")]
    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "Price must be a positive amount.")]
    public decimal ProposedPrice { get; set; }
}
