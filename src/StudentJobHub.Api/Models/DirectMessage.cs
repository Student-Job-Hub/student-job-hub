namespace StudentJobHub.Api.Models;

public class DirectMessage
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public JobApplication? Application { get; set; }

    public string SenderId { get; set; } = string.Empty;

    public ApplicationUser? Sender { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}