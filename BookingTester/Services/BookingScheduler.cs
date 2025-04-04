using BookingTester.Models;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace BookingTester.Services;

public interface IBookingScheduler
{
    Task ScheduleBookingAsync(ClimbingEvent climbingEvent, IEnumerable<Climber> climbers);
    Task<IEnumerable<ScheduledBooking>> GetScheduledBookingsAsync();
    Task CancelScheduledBookingAsync(string jobId);
    Task<IEnumerable<CompletedBooking>> GetCompletedBookingsAsync();
}

public class ScheduledBooking
{
    public string JobId { get; set; } = string.Empty;
    public long EventId { get; set; }
    public string ClimberName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CompletedBooking
{
    public long EventId { get; set; }
    public string ClimberName { get; set; } = string.Empty;
    public DateTime CompletedTime { get; set; }
    public BookStatus Result { get; set; }
}

public class BookingScheduler : IBookingScheduler
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingScheduler> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMonitoringApi _monitoringApi;

    public BookingScheduler(
        IBookingService bookingService,
        ILogger<BookingScheduler> logger,
        IBackgroundJobClient backgroundJobClient,
        IMonitoringApi monitoringApi)
    {
        _bookingService = bookingService;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _monitoringApi = monitoringApi;
    }

    public async Task ScheduleBookingAsync(ClimbingEvent climbingEvent, IEnumerable<Climber> climbers)
    {
        var eventBookableTime = climbingEvent.StartTime - TimeSpan.FromDays(1);
        
        foreach (var climber in climbers)
        {
            var jobId = _backgroundJobClient.Schedule(
                () => ProcessBookingAsync(climber, climbingEvent.Id),
                eventBookableTime);

            _logger.LogInformation(
                "Scheduled booking for {ClimberName} at event {EventId} for {ScheduledTime}",
                climber.Name,
                climbingEvent.Id,
                eventBookableTime);
        }
    }

    public async Task<IEnumerable<ScheduledBooking>> GetScheduledBookingsAsync()
    {
        var scheduledJobs = _monitoringApi.ScheduledJobs(0, int.MaxValue);
        var scheduledBookings = new List<ScheduledBooking>();

        foreach (var job in scheduledJobs)
        {
            var jobDetails = _monitoringApi.JobDetails(job.Key);
            var invocationData = InvocationData.Deserialize(jobDetails.History[0].Data["Arguments"][0].ToString());
            var args = invocationData.Arguments;

            scheduledBookings.Add(new ScheduledBooking
            {
                JobId = job.Key,
                EventId = (long)args[1],
                ClimberName = ((Climber)args[0]).Name,
                ScheduledTime = job.Value.EnqueueAt,
                Status = job.Value.State
            });
        }

        return scheduledBookings;
    }

    public async Task CancelScheduledBookingAsync(string jobId)
    {
        _backgroundJobClient.Delete(jobId);
        _logger.LogInformation("Cancelled scheduled booking {JobId}", jobId);
    }

    public async Task<IEnumerable<CompletedBooking>> GetCompletedBookingsAsync()
    {
        var succeededJobs = _monitoringApi.SucceededJobs(0, int.MaxValue);
        var completedBookings = new List<CompletedBooking>();

        foreach (var job in succeededJobs)
        {
            var jobDetails = _monitoringApi.JobDetails(job.Key);
            var result = jobDetails.History[0].Data["Result"];
            var invocationData = InvocationData.Deserialize(jobDetails.History[0].Data["Arguments"][0].ToString());
            var args = invocationData.Arguments;

            completedBookings.Add(new CompletedBooking
            {
                EventId = (long)args[1],
                ClimberName = ((Climber)args[0]).Name,
                CompletedTime = job.Value.SucceededAt.Value,
                Result = (BookStatus)result
            });
        }

        return completedBookings;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessBookingAsync(Climber climber, long eventId)
    {
        try
        {
            var result = await _bookingService.BookClimberAsync(climber, eventId);
            _logger.LogInformation(
                "Completed booking for {ClimberName} at event {EventId} with result {Result}",
                climber.Name,
                eventId,
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process booking for {ClimberName} at event {EventId}",
                climber.Name,
                eventId);
            throw;
        }
    }
} 