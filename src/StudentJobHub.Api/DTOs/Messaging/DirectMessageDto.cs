namespace StudentJobHub.Api.DTOs.Messaging;

public class DirectMessageDto
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public string SenderId { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }
}