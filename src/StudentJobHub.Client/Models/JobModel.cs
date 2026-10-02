namespace StudentJobHub.Client.Models;

public class JobModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Requirements { get; set; } = string.Empty;

    public decimal Budget { get; set; }

    public DateTime Deadline { get; set; }

    public string PostedById { get; set; } = string.Empty;

    public string PostedByName { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }
   
    public bool IsOpen { get; set; }

    public DateTime CreatedAt { get; set; }
}