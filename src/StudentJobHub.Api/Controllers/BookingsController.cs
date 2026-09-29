using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentJobHub.Api.DTOs.Bookings;
using StudentJobHub.Api.Services;

namespace StudentJobHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly ServiceBookingService _bookingService;

    public BookingsController(ServiceBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceBookingDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _bookingService.CreateBookingAsync(dto, userId);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Booking!.Id },
            result.Booking);
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var bookings = await _bookingService.GetClientBookingsAsync(userId);
        return Ok(bookings);
    }

    [HttpGet("received")]
    public async Task<IActionResult> GetReceived()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var bookings = await _bookingService.GetProviderBookingsAsync(userId);
        return Ok(bookings);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var booking = await _bookingService.GetBookingByIdAsync(id, userId);
        if (booking == null)
        {
            return NotFound(new { message = "Booking request not found or you are not authorized to view it." });
        }

        return Ok(booking);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateServiceBookingStatusDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _bookingService.UpdateStatusAsync(id, dto, userId);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _bookingService.DeleteBookingAsync(id, userId);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
    }
}
