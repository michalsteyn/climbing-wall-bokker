using BookingTester.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BookingTester.Client;

public class MockClimbingBookerOptions
{
    public string EventsFilePath { get; set; } = "events.json";
    public BookStatus DefaultBookingResult { get; set; } = BookStatus.OK;
    public TimeSpan ServerTimeOffset { get; set; } = TimeSpan.Zero;
}

public class MockClimbingBooker : IClimbingBooker
{
    private readonly ILogger<MockClimbingBooker> _logger;
    private readonly MockClimbingBookerOptions _options;
    private List<ClimbingEvent>? _cachedEvents;

    public MockClimbingBooker(
        ILogger<MockClimbingBooker> logger,
        IOptions<MockClimbingBookerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<(List<ClimbingEvent>, TimeSpan?)> GetClimbingEvents(bool includeCertified = false, bool noParse = false)
    {
        try
        {
            if (_cachedEvents == null)
            {
                var dataFile = Path.Combine("Data", _options.EventsFilePath);
                if (!File.Exists(dataFile))
                {
                    _logger.LogWarning("Events file not found at {Path}. Creating default events.", dataFile);
                    _cachedEvents = CreateDefaultEvents();
                    await SaveEventsToFileAsync(_cachedEvents);
                }
                else
                {
                    var json = await File.ReadAllTextAsync(dataFile);
                    _cachedEvents = JsonSerializer.Deserialize<List<ClimbingEvent>>(json) ?? new List<ClimbingEvent>();
                }
            }

            _cachedEvents[0].StartTime = DateTime.Now + TimeSpan.FromHours(24) + TimeSpan.FromSeconds(15);
            _cachedEvents[0].EndTime = _cachedEvents[0].StartTime + TimeSpan.FromHours(1);

            var filteredEvents = _cachedEvents.Where(e => e.StartTime >= DateTime.Now).ToList();
            return (filteredEvents, _options.ServerTimeOffset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading events from file");
            return (new List<ClimbingEvent>(), _options.ServerTimeOffset);
        }
    }

    public async Task<bool> LogIn(string name, string user, string pass)
    {
        _logger.LogInformation("Mock login for {Name}", name);
        return true;
    }

    public async Task<BookStatus> BookClimb(long eventId, string name)
    {
        _logger.LogInformation("Mock booking for {Name} at event {EventId}", name, eventId);
        return _options.DefaultBookingResult;
    }

    public async Task<BookStatus> BookClimb(string name, string user, string pass, long eventId)
    {
        _logger.LogInformation("Mock booking for {Name} at event {EventId}", name, eventId);
        return _options.DefaultBookingResult;
    }

    public async Task<BookStatus> CheckBooking(long eventId, string name)
    {
        _logger.LogInformation("Mock checking booking for {Name} at event {EventId}", name, eventId);
        return _options.DefaultBookingResult;
    }

    private List<ClimbingEvent> CreateDefaultEvents()
    {
        var tomorrow = DateTime.Now.Date.AddDays(1);
        return new List<ClimbingEvent>
        {
            new()
            {
                Id = 1,
                StartTime = tomorrow.AddHours(18),
                EndTime = tomorrow.AddHours(20),
                Title = "Mock Evening Climb",
                Description = "A mock climbing event for testing",
                Capacity = 10,
                Booked = 0
            },
            new()
            {
                Id = 2,
                StartTime = tomorrow.AddDays(1).AddHours(18),
                EndTime = tomorrow.AddDays(1).AddHours(20),
                Title = "Mock Evening Climb (Next Day)",
                Description = "A mock climbing event for testing",
                Capacity = 10,
                Booked = 0
            }
        };
    }

    private async Task SaveEventsToFileAsync(List<ClimbingEvent> events)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(events, options);
            await File.WriteAllTextAsync(_options.EventsFilePath, json);
            _logger.LogInformation("Default events saved to {Path}", _options.EventsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving default events to file");
        }
    }
} 