using BookingTester.Models;
using Microsoft.Extensions.Logging;

namespace BookingTester.Services;

public interface IBookingService
{
    Task<ClimbingEvent?> FindNextAvailableEventAsync(DateTime targetDate, TimeSpan targetTime);
    Task<BookStatus> BookClimberAsync(Climber climber, long eventId);
    Task WaitUntilBookingTimeAsync(DateTime eventBookableTime, TimeSpan serverTimeOffset);
}

public class BookingService : IBookingService
{
    private readonly IClimbingBooker _climbingBooker;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IClimbingBooker climbingBooker, ILogger<BookingService> logger)
    {
        _climbingBooker = climbingBooker;
        _logger = logger;
    }

    public async Task<ClimbingEvent?> FindNextAvailableEventAsync(DateTime targetDate, TimeSpan targetTime)
    {
        var (climbingEvents, serverTime) = await _climbingBooker.GetClimbingEvents(false);
        var serverTimeOffset = Math.Max(0, serverTime?.TotalSeconds ?? 0);

        return climbingEvents
            .Where(e => e.StartTime.Date == targetDate && e.StartTime.TimeOfDay >= targetTime)
            .OrderBy(e => e.StartTime)
            .FirstOrDefault();
    }

    public async Task<BookStatus> BookClimberAsync(Climber climber, long eventId)
    {
        try
        {
            _logger.LogInformation("Attempting to book climb for {ClimberName}, Event ID: {EventId}", climber.Name, eventId);
            var result = await _climbingBooker.BookClimb(climber.Name, climber.Email, climber.Password, eventId);
            _logger.LogInformation("Booking result for {ClimberName}: {Result}", climber.Name, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking climb for {ClimberName}, Event ID: {EventId}", climber.Name, eventId);
            throw;
        }
    }

    public async Task WaitUntilBookingTimeAsync(DateTime eventBookableTime, TimeSpan serverTimeOffset)
    {
        var adjustedLocalTime = DateTime.Now + serverTimeOffset;

        if (eventBookableTime > adjustedLocalTime)
        {
            var waitTime = eventBookableTime - adjustedLocalTime;
            _logger.LogInformation("Waiting {WaitTime} seconds until booking time", waitTime.TotalSeconds);

            while (true)
            {
                var remainingTime = eventBookableTime - (DateTime.Now + serverTimeOffset);

                if (remainingTime <= TimeSpan.Zero)
                    break;

                _logger.LogInformation("Remaining time to book: {RemainingTime:F1} seconds", remainingTime.TotalSeconds);

                var sleepTime = remainingTime > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : remainingTime;
                await Task.Delay(sleepTime);
            }
        }
    }
} 