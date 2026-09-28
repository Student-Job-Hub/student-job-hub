using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Messaging;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Hubs;

[Authorize]
public class DirectMessageHub : Hub
{
    private readonly ApplicationDbContext _context;

    public DirectMessageHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DirectMessageDto>> JoinConversation(int applicationId)
    {
        await GetParticipantApplicationAsync(applicationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(applicationId));

        return await _context.DirectMessages
            .AsNoTracking()
            .Where(message => message.ApplicationId == applicationId)
            .OrderBy(message => message.SentAt)
            .Select(message => new DirectMessageDto
            {
                Id = message.Id,
                ApplicationId = message.ApplicationId,
                SenderId = message.SenderId,
                SenderName = message.Sender!.FullName,
                Content = message.Content,
                SentAt = message.SentAt
            })
            .ToListAsync();
    }

    public async Task SendMessage(int applicationId, string content)
    {
        var application = await GetParticipantApplicationAsync(applicationId);
        var messageContent = content?.Trim();

        if (string.IsNullOrWhiteSpace(messageContent) || messageContent.Length > 2000)
        {
            throw new HubException("Messages must be between 1 and 2000 characters.");
        }

        var senderId = GetCurrentUserId();
        var sender = await _context.Users
            .AsNoTracking()
            .FirstAsync(user => user.Id == senderId);
        var message = new DirectMessage
        {
            ApplicationId = application.Id,
            SenderId = senderId,
            Content = messageContent,
            SentAt = DateTime.UtcNow
        };

        _context.DirectMessages.Add(message);
        await _context.SaveChangesAsync();

        await Clients.Group(GetGroupName(applicationId))
            .SendAsync("ReceiveMessage", new DirectMessageDto
            {
                Id = message.Id,
                ApplicationId = message.ApplicationId,
                SenderId = senderId,
                SenderName = sender.FullName,
                Content = message.Content,
                SentAt = message.SentAt
            });
    }

    private async Task<JobApplication> GetParticipantApplicationAsync(
        int applicationId)
    {
        var userId = GetCurrentUserId();
        var application = await _context.JobApplications
            .Include(item => item.Job)
            .FirstOrDefaultAsync(item => item.Id == applicationId);

        if (application?.Job == null ||
            (application.ApplicantId != userId && application.Job.PostedById != userId))
        {
            throw new HubException("You are not a participant in this conversation.");
        }

        return application;
    }

    private string GetCurrentUserId()
    {
        return Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("You must be signed in to use messaging.");
    }

    private static string GetGroupName(int applicationId) => $"application-{applicationId}";
}