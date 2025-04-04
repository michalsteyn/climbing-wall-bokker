using BookingTester.Models;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

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
        IBackgroundJobClient backgroundJobClient)
    {
        _bookingService = bookingService;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        //_monitoringApi = monitoringApi;
        _monitoringApi = JobStorage.Current.GetMonitoringApi();
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
            var climber = jobDetails.Job.Args[0] as Climber;
            var eventId = jobDetails.Job.Args[1] as long?;

            var history = jobDetails.History.FirstOrDefault();
            //if (history == null) continue;

            //var args = JsonConvert.DeserializeObject<object[]>(history.Data["Arguments"].ToString());
            //if (args == null || args.Length < 2) continue;

            //var climber = JsonConvert.DeserializeObject<Climber>(args[0].ToString());
            if (climber == null) continue;

            scheduledBookings.Add(new ScheduledBooking
            {
                JobId = job.Key,
                EventId = eventId ?? 0,
                ClimberName = climber.Name,
                ScheduledTime = job.Value.EnqueueAt,
                Status = history?.StateName ?? "Unknown"
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
            var climber = jobDetails.Job.Args[0] as Climber;
            var eventId = jobDetails.Job.Args[1] as long?;

            var history = jobDetails.History.FirstOrDefault();
            if (climber == null) continue;

            var result = JsonConvert.DeserializeObject<BookStatus>(history.Data["Result"].ToString());

            completedBookings.Add(new CompletedBooking
            {
                EventId = eventId ?? 0,
                ClimberName = climber.Name,
                CompletedTime = job.Value.SucceededAt ?? DateTime.MinValue,
                Result = result
            });
        }

        return completedBookings;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task<BookStatus> ProcessBookingAsync(Climber climber, long eventId)
    {
        try
        {
            var result = await _bookingService.BookClimberAsync(climber, eventId);
            _logger.LogInformation(
                "Completed booking for {ClimberName} at event {EventId} with result {Result}",
                climber.Name,
                eventId,
                result);
            return result;
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