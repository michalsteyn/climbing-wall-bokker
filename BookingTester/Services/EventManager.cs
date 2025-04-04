using BookingTester.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BookingTester.Services;

public class EventSelectionOptions
{
    public TimeSpan TargetTime { get; set; } = TimeSpan.FromHours(18);
    public int DaysAhead { get; set; } = 1;
}

public interface IEventManager
{
    Task<IEnumerable<ClimbingEvent>> GetAllEventsAsync(bool includePastEvents = false);
    Task<ClimbingEvent?> FindNextAvailableEventAsync();
    Task<ClimbingEvent?> FindNextAvailableEventAsync(DateTime targetDate, TimeSpan targetTime);
    Task<DateTime> GetEventBookableTimeAsync(ClimbingEvent climbingEvent);
    Task<TimeSpan> GetServerTimeOffsetAsync();
    Task WaitUntilBookingTimeAsync(DateTime eventBookableTime);
}

public class EventManager : IEventManager
{
    private readonly IClimbingBooker _climbingBooker;
    private readonly ILogger<EventManager> _logger;
    private readonly EventSelectionOptions _options;

    public EventManager(
        IClimbingBooker climbingBooker, 
        ILogger<EventManager> logger,
        IOptions<EventSelectionOptions> options)
    {
        _climbingBooker = climbingBooker;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IEnumerable<ClimbingEvent>> GetAllEventsAsync(bool includePastEvents = false)
    {
        var (climbingEvents, _) = await _climbingBooker.GetClimbingEvents(includePastEvents);
        return climbingEvents;
    }

    public async Task<ClimbingEvent?> FindNextAvailableEventAsync()
    {
        var targetDate = DateTime.Now.Date.AddDays(_options.DaysAhead);
        return await FindNextAvailableEventAsync(targetDate, _options.TargetTime);
    }

    public async Task<ClimbingEvent?> FindNextAvailableEventAsync(DateTime targetDate, TimeSpan targetTime)
    {
        var climbingEvents = await GetAllEventsAsync(false);
        
        var nextEvent = climbingEvents
            .Where(e => e.StartTime.Date == targetDate && e.StartTime.TimeOfDay >= targetTime)
            .OrderBy(e => e.StartTime)
            .FirstOrDefault();

        if (nextEvent == null)
        {
            _logger.LogWarning("No suitable climbing event found for {TargetDate} at {TargetTime}", 
                targetDate.ToShortDateString(), targetTime);
        }

        return nextEvent;
    }

    public async Task<DateTime> GetEventBookableTimeAsync(ClimbingEvent climbingEvent)
    {
        return climbingEvent.StartTime - TimeSpan.FromDays(1);
    }

    public async Task<TimeSpan> GetServerTimeOffsetAsync()
    {
        var (_, serverTime) = await _climbingBooker.GetClimbingEvents(false);
        return TimeSpan.FromSeconds(Math.Max(0, serverTime?.TotalSeconds ?? 0));
    }

    public async Task WaitUntilBookingTimeAsync(DateTime eventBookableTime)
    {
        var serverTimeOffset = await GetServerTimeOffsetAsync();
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