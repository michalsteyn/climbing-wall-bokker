using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingTester.Services;

public class BookingWorker : BackgroundService
{
    private readonly IUserManager _userManager;
    private readonly IEventManager _eventManager;
    private readonly IBookingScheduler _bookingScheduler;
    private readonly ILogger<BookingWorker> _logger;

    public BookingWorker(
        IUserManager userManager,
        IEventManager eventManager,
        IBookingScheduler bookingScheduler,
        ILogger<BookingWorker> logger)
    {
        _userManager = userManager;
        _eventManager = eventManager;
        _bookingScheduler = bookingScheduler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Get all users
            var climbers = await _userManager.GetClimbersAsync();
            _logger.LogInformation("Found {Count} climbers to book", climbers.Count());

            // Find the next available event
            var climbingEvent = await _eventManager.FindNextAvailableEventAsync();

            if (climbingEvent == null)
            {
                _logger.LogWarning("No suitable climbing event found");
                return;
            }

            // Schedule bookings for all climbers
            await _bookingScheduler.ScheduleBookingAsync(climbingEvent, climbers);
            _logger.LogInformation("Scheduled bookings for event {EventId}", climbingEvent.Id);

            // Display scheduled bookings
            var scheduledBookings = await _bookingScheduler.GetScheduledBookingsAsync();
            foreach (var booking in scheduledBookings)
            {
                _logger.LogInformation(
                    "Scheduled booking: {ClimberName} for event {EventId} at {ScheduledTime}",
                    booking.ClimberName,
                    booking.EventId,
                    booking.ScheduledTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in booking worker");
        }
    }
} 