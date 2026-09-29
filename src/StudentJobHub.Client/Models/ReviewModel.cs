using System.ComponentModel.DataAnnotations;

namespace StudentJobHub.Client.Models;

public class ReviewModel
{
    public int Id { get; set; }

    public string ReviewerId { get; set; } = string.Empty;

    public string ReviewerName { get; set; } = string.Empty;

    public string RevieweeId { get; set; } = string.Empty;

    public string RevieweeName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CreateReviewModel
{
    [Required]
    public string RevieweeId { get; set; } = string.Empty;

    [Required]
    [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5 stars.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Please enter a comment sharing your experience.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Comment must be between 3 and 1000 characters.")]
    public string Comment { get; set; } = string.Empty;
}

public class ReviewSummaryModel
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int Star5Count { get; set; }
    public int Star4Count { get; set; }
    public int Star3Count { get; set; }
    public int Star2Count { get; set; }
    public int Star1Count { get; set; }

    public static ReviewSummaryModel FromReviews(IEnumerable<ReviewModel> reviews)
    {
        var list = reviews?.ToList() ?? new List<ReviewModel>();
        if (!list.Any())
        {
            return new ReviewSummaryModel();
        }

        return new ReviewSummaryModel
        {
            TotalReviews = list.Count,
            AverageRating = Math.Round(list.Average(r => r.Rating), 1),
            Star5Count = list.Count(r => r.Rating == 5),
            Star4Count = list.Count(r => r.Rating == 4),
            Star3Count = list.Count(r => r.Rating == 3),
            Star2Count = list.Count(r => r.Rating == 2),
            Star1Count = list.Count(r => r.Rating == 1)
        };
    }
}