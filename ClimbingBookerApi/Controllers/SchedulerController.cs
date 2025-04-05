using BookingTester.Models;
using BookingTester.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClimbingBookerApi.Controllers;

/// <summary>
/// Controller for managing scheduled bookings and viewing booking status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
public class SchedulerController : ControllerBase
{
    private readonly IBookingScheduler _bookingScheduler;
    private readonly ILogger<SchedulerController> _logger;

    public SchedulerController(IBookingScheduler bookingScheduler, ILogger<SchedulerController> logger)
    {
        _bookingScheduler = bookingScheduler;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all currently scheduled bookings.
    /// </summary>
    /// <returns>A list of scheduled bookings.</returns>
    /// <response code="200">Returns the list of scheduled bookings.</response>
    [HttpGet("scheduled")]
    [ProducesResponseType(typeof(IEnumerable<ScheduledBooking>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScheduledBooking>>> GetScheduledBookings()
    {
        _logger.LogInformation("Retrieving scheduled bookings");
        var scheduledBookings = await _bookingScheduler.GetScheduledBookingsAsync();
        return Ok(scheduledBookings);
    }

    /// <summary>
    /// Retrieves all completed bookings.
    /// </summary>
    /// <returns>A list of completed bookings.</returns>
    /// <response code="200">Returns the list of completed bookings.</response>
    [HttpGet("completed")]
    [ProducesResponseType(typeof(IEnumerable<CompletedBooking>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CompletedBooking>>> GetCompletedBookings()
    {
        _logger.LogInformation("Retrieving completed bookings");
        var completedBookings = await _bookingScheduler.GetCompletedBookingsAsync();
        return Ok(completedBookings);
    }

    /// <summary>
    /// Cancels a scheduled booking.
    /// </summary>
    /// <param name="jobId">The ID of the scheduled booking to cancel.</param>
    /// <returns>The result of the cancellation operation.</returns>
    /// <response code="200">Booking was successfully cancelled.</response>
    /// <response code="400">If the booking is not found or cannot be cancelled.</response>
    [HttpDelete("{jobId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelBooking(string jobId)
    {
        _logger.LogInformation("Cancelling booking {JobId}", jobId);
        await _bookingScheduler.CancelScheduledBookingAsync(jobId);
        return Ok("Booking cancelled successfully");
    }

    /// <summary>
    /// Removes old jobs from the scheduler that are older than the specified time.
    /// </summary>
    /// <param name="days">The number of days to keep jobs. Jobs older than this will be removed.
    /// If set to 0, all jobs will be removed regardless of age.</param>
    /// <returns>The number of jobs that were removed.</returns>
    /// <response code="200">Returns the number of jobs that were removed.</response>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> CleanupOldJobs([FromQuery] int days = 7)
    {
        _logger.LogInformation("Cleaning up jobs older than {Days} days", days);
        var removedCount = await _bookingScheduler.CleanupOldJobsAsync(TimeSpan.FromDays(days));
        return Ok(removedCount);
    }
} 