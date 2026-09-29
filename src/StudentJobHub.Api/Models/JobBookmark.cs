namespace StudentJobHub.Api.Models;

public class JobBookmark
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public Job? Job { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
