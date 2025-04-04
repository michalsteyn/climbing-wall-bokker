using BookingTester.Models;
using BookingTester.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClimbingBookerApi.Controllers;

/// <summary>
/// Controller for managing climbing event bookings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
public class BookingController : ControllerBase
{
    private readonly IBookingScheduler _bookingScheduler;
    private readonly IEventManager _eventManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IBookingScheduler bookingScheduler,
        IEventManager eventManager,
        IUserManager userManager,
        ILogger<BookingController> logger)
    {
        _bookingScheduler = bookingScheduler;
        _eventManager = eventManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Schedules bookings for multiple climbers for a specific event.
    /// </summary>
    /// <param name="eventId">The ID of the event to book.</param>
    /// <param name="climberIds">An array of climber IDs to book for the event.</param>
    /// <returns>The result of the scheduling operation.</returns>
    /// <response code="200">Bookings were successfully scheduled.</response>
    /// <response code="400">If the event or any climber is not found.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleBooking(long eventId, [FromBody] long[] climberIds)
    {
        _logger.LogInformation("Scheduling booking for event {EventId} and climbers {ClimberIds}", 
            eventId, string.Join(", ", climberIds));

        var @event = await _eventManager.GetEventByIdAsync(eventId);
        if (@event == null)
        {
            return BadRequest($"Event with ID {eventId} not found");
        }

        var climbers = new List<Climber>();
        foreach (var climberId in climberIds)
        {
            var climber = await _userManager.GetClimberByIdAsync(climberId);
            if (climber == null)
            {
                return BadRequest($"Climber with ID {climberId} not found");
            }
            climbers.Add(climber);
        }

        await _bookingScheduler.ScheduleBookingAsync(@event, climbers);
        return Ok($"Successfully scheduled bookings for {climbers.Count} climbers");
    }
} 