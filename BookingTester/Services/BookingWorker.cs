using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingTester.Services;

public class BookingWorker : BackgroundService
{
    private readonly IBookingService _bookingService;
    private readonly IUserManager _userManager;
    private readonly ILogger<BookingWorker> _logger;

    public BookingWorker(
        IBookingService bookingService,
        IUserManager userManager,
        ILogger<BookingWorker> logger)
    {
        _bookingService = bookingService;
        _userManager = userManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var climbers = await _userManager.GetClimbersAsync();
            var oneDayFromNow = DateTime.Now.Date.AddDays(1);
            var targetTime = TimeSpan.FromHours(18);

            var climbingEvent = await _bookingService.FindNextAvailableEventAsync(oneDayFromNow, targetTime);
            if (climbingEvent == null)
            {
                _logger.LogWarning("No suitable climbing event found for tomorrow at {TargetTime}", targetTime);
                return;
            }

            var eventBookableTime = climbingEvent.StartTime - TimeSpan.FromDays(1);
            var serverTimeOffset = TimeSpan.FromSeconds(0); // This should be obtained from the booking service

            await _bookingService.WaitUntilBookingTimeAsync(eventBookableTime, serverTimeOffset);

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