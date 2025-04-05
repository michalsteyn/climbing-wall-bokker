using BookingTester.Models;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BookingTester.Services;

public interface IBookingScheduler
{
    Task ScheduleBookingAsync(ClimbingEvent climbingEvent, IEnumerable<Climber> climbers);
    Task<IEnumerable<ScheduledBooking>> GetScheduledBookingsAsync();
    Task CancelScheduledBookingAsync(string jobId);
    Task<IEnumerable<BookingResult>> GetCompletedBookingsAsync();
    Task<int> CleanupOldJobsAsync(TimeSpan olderThan);
}

public class ScheduledBooking
{
    public string JobId { get; set; } = string.Empty;
    public long EventId { get; set; }
    public string ClimberName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BookingScheduler : IBookingScheduler
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingScheduler> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMonitoringApi _monitoringApi;

    // Timing constants
    private const int EarlySchedulingSeconds = 30; // Schedule job 30 seconds early
    private const int ImmediateExecutionThresholdSeconds = 40; // Execute immediately if less than 40 seconds away
    private const int PollingIntervalSeconds = 10; // Hangfire polling interval

    public BookingScheduler(
        IBookingService bookingService,
        ILogger<BookingScheduler> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _bookingService = bookingService;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _monitoringApi = JobStorage.Current.GetMonitoringApi();
    }

    public async Task ScheduleBookingAsync(ClimbingEvent climbingEvent, IEnumerable<Climber> climbers)
    {
        var eventBookableTime = climbingEvent.StartTime - TimeSpan.FromDays(1);
        var now = DateTime.UtcNow;
        var timeUntilBooking = eventBookableTime - now;

        // If the booking time is less than the immediate execution threshold, schedule for immediate execution
        var scheduledTime = timeUntilBooking.TotalSeconds <= ImmediateExecutionThresholdSeconds
            ? now
            : eventBookableTime.AddSeconds(-EarlySchedulingSeconds);

        foreach (var climber in climbers)
        {
            var jobId = _backgroundJobClient.Schedule(
                () => ProcessBookingAsync(climber, climbingEvent.Id, eventBookableTime),
                scheduledTime);

            _logger.LogInformation(
                "Scheduled booking for {ClimberName} at event {EventId}. Actual booking time: {BookingTime}, Scheduled execution: {ScheduledTime}",
                climber.Name,
                climbingEvent.Id,
                eventBookableTime,
                scheduledTime);
        }
    }

    public async Task<IEnumerable<ScheduledBooking>> GetScheduledBookingsAsync()
    {
        var scheduledJobs = _monitoringApi.ScheduledJobs(0, int.MaxValue);
        var scheduledBookings = new List<ScheduledBooking>();

        foreach (var job in scheduledJobs)
        {
            var jobDetails = _monitoringApi.JobDetails(job.Key);
            var climber = jobDetails.Job.Args[0] as Climber;
            var eventId = jobDetails.Job.Args[1] as long?;
            var targetBookingTime = (DateTime)jobDetails.Job.Args[2];

            if (climber == null) continue;

            scheduledBookings.Add(new ScheduledBooking
            {
                JobId = job.Key,
                EventId = eventId ?? 0,
                ClimberName = climber.Name,
                ScheduledTime = targetBookingTime,
                Status = jobDetails.History.FirstOrDefault()?.StateName ?? "Unknown"
            });
        }

        return scheduledBookings;
    }

    public async Task CancelScheduledBookingAsync(string jobId)
    {
        _backgroundJobClient.Delete(jobId);
        _logger.LogInformation("Cancelled scheduled booking {JobId}", jobId);
    }

    public async Task<IEnumerable<BookingResult>> GetCompletedBookingsAsync()
    {
        var succeededJobs = _monitoringApi.SucceededJobs(0, int.MaxValue);
        var completedBookings = new List<BookingResult>();

        foreach (var job in succeededJobs)
        {
            var jobDetails = _monitoringApi.JobDetails(job.Key);
            var history = jobDetails.History.FirstOrDefault();
            if (history == null) continue;

            var result = JsonConvert.DeserializeObject<BookingResult>(history.Data["Result"].ToString());
            if (result != null)
            {
                completedBookings.Add(result);
            }
        }

        return completedBookings;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task<BookingResult> ProcessBookingAsync(Climber climber, long eventId, DateTime targetBookingTime)
    {
        try
        {
            var now = DateTime.UtcNow;
            var timeUntilBooking = targetBookingTime - now;

            if (timeUntilBooking > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "Waiting {WaitTime}ms before executing booking for {ClimberName} at event {EventId}",
                    timeUntilBooking.TotalMilliseconds,
                    climber.Name,
                    eventId);
                
                await Task.Delay(timeUntilBooking);
            }

            var result = await _bookingService.BookClimberAsync(climber, eventId);
            _logger.LogInformation(
                "Completed booking for {ClimberName} at event {EventId} with result {Result} after {RetryCount} retries",
                climber.Name,
                eventId,
                result.Status,
                result.RetryCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process booking for {ClimberName} at event {EventId}",
                climber.Name,
                eventId);
            return new BookingResult
            {
                Status = BookStatus.Error,
                User = new UserDto
                {
                    Id = climber.Id,
                    Name = climber.Name
                },
                EventId = eventId,
                RetryCount = 0,
                CompletedAt = DateTime.Now,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Removes old jobs from Hangfire storage that are older than the specified time.
    /// </summary>
    /// <param name="olderThan">The age threshold for jobs to be removed.
    /// If set to TimeSpan.Zero, all jobs will be removed regardless of age.</param>
    /// <returns>The number of jobs that were removed.</returns>
    public async Task<int> CleanupOldJobsAsync(TimeSpan olderThan)
    {
        _logger.LogInformation("Cleaning up jobs older than {OlderThan}", olderThan);
        
        var cutoffDate = olderThan == TimeSpan.Zero 
            ? DateTime.MaxValue 
            : DateTime.UtcNow.Subtract(olderThan);
        
        var removedCount = 0;

        // Clean up scheduled jobs
        var scheduledJobs = _monitoringApi.ScheduledJobs(0, int.MaxValue);
        foreach (var job in scheduledJobs)
        {
            if (olderThan == TimeSpan.Zero || job.Value.EnqueueAt < cutoffDate)
            {
                _backgroundJobClient.Delete(job.Key);
                removedCount++;
            }
        }

        // Clean up completed jobs
        var succeededJobs = _monitoringApi.SucceededJobs(0, int.MaxValue);
        foreach (var job in succeededJobs)
        {
            if (olderThan == TimeSpan.Zero || job.Value.SucceededAt < cutoffDate)
            {
                _backgroundJobClient.Delete(job.Key);
                removedCount++;
            }
        }

        _logger.LogInformation("Removed {Count} old jobs", removedCount);
        return removedCount;
    }
} 