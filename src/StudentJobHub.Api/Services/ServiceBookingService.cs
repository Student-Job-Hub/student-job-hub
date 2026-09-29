using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.DTOs.Bookings;
using StudentJobHub.Api.Hubs;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Services;

public class ServiceBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ServiceBookingService(
        ApplicationDbContext context,
        NotificationService notificationService,
        IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    public async Task<(bool Success, string Message, ServiceBookingResponseDto? Booking)> CreateBookingAsync(
        CreateServiceBookingDto dto,
        string clientId)
    {
        var service = await _context.Services
            .Include(s => s.Provider)
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId);

        if (service == null)
        {
            return (false, "Service not found.", null);
        }

        if (service.ProviderId == clientId)
        {
            return (false, "You cannot book your own service.", null);
        }

        var price = dto.ProposedPrice > 0 ? dto.ProposedPrice : service.Price;

        var booking = new ServiceBooking
        {
            ServiceId = dto.ServiceId,
            ClientId = clientId,
            ProviderId = service.ProviderId,
            RequestedDate = dto.RequestedDate,
            LocationOrDelivery = string.IsNullOrWhiteSpace(dto.LocationOrDelivery) ? "On Campus / Remote" : dto.LocationOrDelivery.Trim(),
            Notes = dto.Notes.Trim(),
            ProposedPrice = price,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceBookings.Add(booking);
        await _context.SaveChangesAsync();

        var client = await _context.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        var clientName = client?.FullName ?? "A student";

        // Notify Provider
        var notification = await _notificationService.CreateAsync(
            service.ProviderId,
            $"{clientName} requested a booking for your service: '{service.Title}'.");

        await _hubContext.Clients
            .Group($"user-{service.ProviderId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Message,
                notification.IsRead,
                notification.CreatedAt
            });

        var response = await GetBookingByIdAsync(booking.Id, clientId);
        return (true, "Booking request submitted successfully.", response);
    }

    public async Task<List<ServiceBookingResponseDto>> GetClientBookingsAsync(string clientId)
    {
        return await _context.ServiceBookings
            .Include(b => b.Service)
            .Include(b => b.Provider)
            .Include(b => b.Client)
            .Where(b => b.ClientId == clientId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => MapToDto(b))
            .ToListAsync();
    }

    public async Task<List<ServiceBookingResponseDto>> GetProviderBookingsAsync(string providerId)
    {
        return await _context.ServiceBookings
            .Include(b => b.Service)
            .Include(b => b.Provider)
            .Include(b => b.Client)
            .Where(b => b.ProviderId == providerId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => MapToDto(b))
            .ToListAsync();
    }

    public async Task<ServiceBookingResponseDto?> GetBookingByIdAsync(int id, string userId)
    {
        var booking = await _context.ServiceBookings
            .Include(b => b.Service)
            .Include(b => b.Provider)
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return null;

        if (booking.ClientId != userId && booking.ProviderId != userId)
        {
            return null;
        }

        return MapToDto(booking);
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(
        int id,
        UpdateServiceBookingStatusDto dto,
        string userId)
    {
        var booking = await _context.ServiceBookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Include(b => b.Provider)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return (false, "Booking not found.");
        }

        var isProvider = booking.ProviderId == userId;
        var isClient = booking.ClientId == userId;

        if (!isProvider && !isClient)
        {
            return (false, "You are not authorized to update this booking.");
        }

        var targetStatus = dto.Status.Trim();

        if (isProvider)
        {
            var validStatuses = new[] { "Accepted", "Declined", "Completed", "Pending" };
            var matched = validStatuses.FirstOrDefault(s => s.Equals(targetStatus, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                return (false, "Invalid status. Providers can set: Accepted, Declined, Completed, Pending.");
            }

            booking.Status = matched;
            if (!string.IsNullOrWhiteSpace(dto.ProviderResponse))
            {
                booking.ProviderResponse = dto.ProviderResponse.Trim();
            }
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify Client
            var serviceTitle = booking.Service?.Title ?? "Service";
            var notification = await _notificationService.CreateAsync(
                booking.ClientId,
                $"Your booking request for '{serviceTitle}' was updated to '{matched}'.");

            await _hubContext.Clients
                .Group($"user-{booking.ClientId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Message,
                    notification.IsRead,
                    notification.CreatedAt
                });

            return (true, $"Booking status updated to {matched}.");
        }
        else // isClient
        {
            if (!targetStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Clients can only cancel pending or accepted bookings.");
            }

            if (booking.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                booking.Status.Equals("Declined", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Cannot cancel a booking that is already {booking.Status.ToLower()}.");
            }

            booking.Status = "Cancelled";
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify Provider
            var clientName = booking.Client?.FullName ?? "Client";
            var serviceTitle = booking.Service?.Title ?? "Service";
            var notification = await _notificationService.CreateAsync(
                booking.ProviderId,
                $"{clientName} cancelled their booking for '{serviceTitle}'.");

            await _hubContext.Clients
                .Group($"user-{booking.ProviderId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Message,
                    notification.IsRead,
                    notification.CreatedAt
                });

            return (true, "Booking cancelled successfully.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteBookingAsync(int id, string userId)
    {
        var booking = await _context.ServiceBookings
            .FirstOrDefaultAsync(b => b.Id == id && (b.ClientId == userId || b.ProviderId == userId));

        if (booking == null)
        {
            return (false, "Booking not found or you are not authorized to delete it.");
        }

        if (booking.ClientId == userId && booking.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Please cancel the accepted booking before deleting it.");
        }

        _context.ServiceBookings.Remove(booking);
        await _context.SaveChangesAsync();

        return (true, "Booking deleted successfully.");
    }

    private static ServiceBookingResponseDto MapToDto(ServiceBooking b)
    {
        return new ServiceBookingResponseDto
        {
            Id = b.Id,
            ServiceId = b.ServiceId,
            ServiceTitle = b.Service != null ? b.Service.Title : string.Empty,
            ServiceCategory = b.Service != null ? b.Service.Category : string.Empty,
            ClientId = b.ClientId,
            ClientName = b.Client != null ? b.Client.FullName : string.Empty,
            ClientEmail = b.Client != null ? b.Client.Email ?? string.Empty : string.Empty,
            ClientUniversity = b.Client != null ? b.Client.University : null,
            ProviderId = b.ProviderId,
            ProviderName = b.Provider != null ? b.Provider.FullName : string.Empty,
            ProviderEmail = b.Provider != null ? b.Provider.Email ?? string.Empty : string.Empty,
            RequestedDate = b.RequestedDate,
            LocationOrDelivery = b.LocationOrDelivery,
            Notes = b.Notes,
            ProposedPrice = b.ProposedPrice,
            Status = b.Status,
            ProviderResponse = b.ProviderResponse,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
