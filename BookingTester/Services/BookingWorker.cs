using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingTester.Services;

public class BookingWorker : BackgroundService
{
    private readonly IBookingService _bookingService;
    private readonly IUserManager _userManager;
    private readonly IEventManager _eventManager;
    private readonly ILogger<BookingWorker> _logger;

    public BookingWorker(
        IBookingService bookingService,
        IUserManager userManager,
        IEventManager eventManager,
        ILogger<BookingWorker> logger)
    {
        _bookingService = bookingService;
        _userManager = userManager;
        _eventManager = eventManager;
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

            // Wait until the event is bookable
            var eventBookableTime = await _eventManager.GetEventBookableTimeAsync(climbingEvent);
            await _eventManager.WaitUntilBookingTimeAsync(eventBookableTime);

            // Book the event for all climbers
            foreach (var climber in climbers)
            {
                try
                {
                    await _bookingService.BookClimberAsync(climber, climbingEvent.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to book climb for {ClimberName}", climber.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in booking worker");
        }
    }
} 