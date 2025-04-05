using BookingTester.Models;
using Microsoft.Extensions.Logging;

namespace BookingTester.Services;

public interface IBookingService
{
    Task<BookingResult> BookClimberAsync(Climber climber, long eventId);
}

public class BookingService : IBookingService
{
    private readonly IClimbingBooker _climbingBooker;
    private readonly ILogger<BookingService> _logger;
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 1000;

    public BookingService(IClimbingBooker climbingBooker, ILogger<BookingService> logger)
    {
        _climbingBooker = climbingBooker;
        _logger = logger;
    }

    public async Task<BookingResult> BookClimberAsync(Climber climber, long eventId)
    {
        var result = new BookingResult
        {
            User = new UserDto
            {
                Id = climber.Id,
                Name = climber.Name
            },
            EventId = eventId,
            RetryCount = 0,
            CompletedAt = DateTime.Now
        };

        try
        {
            _logger.LogInformation("Attempting to book climb for {ClimberName}, Event ID: {EventId}", climber.Name, eventId);

            for (int i = 0; i < MaxRetries; i++)
            {
                result.RetryCount = i;
                var bookingResult = await _climbingBooker.BookClimb(climber.Name, climber.Email, climber.Password, eventId);
                _logger.LogInformation("Booking result for {ClimberName}: {Result} (Attempt {Attempt})", climber.Name, bookingResult, i + 1);

                switch (bookingResult)
                {
                    case BookStatus.OK:
                    case BookStatus.Waitlisted:
                        var bookingCheck = await _climbingBooker.CheckBooking(eventId, climber.Name);
                        if (bookingCheck == BookStatus.OK || bookingCheck == BookStatus.Waitlisted)
                        {
                            result.Status = bookingCheck;
                            result.CompletedAt = DateTime.Now;
                            return result;
                        }
                        break;

                    case BookStatus.AlreadyBooked:
                        var checkResult = await _climbingBooker.CheckBooking(eventId, climber.Name);
                        result.Status = checkResult;
                        result.CompletedAt = DateTime.Now;
                        return result;

                    case BookStatus.TooEarly:
                        _logger.LogInformation("Booking too early for {ClimberName}, retrying in {Delay}ms", climber.Name, RetryDelayMs);
                        break;

                    case BookStatus.Error:
                        _logger.LogWarning("Error booking for {ClimberName}, retrying in {Delay}ms", climber.Name, RetryDelayMs);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(bookingResult), $"Unexpected booking result: {bookingResult}");
                }

                if (i < MaxRetries - 1)
                {
                    await Task.Delay(RetryDelayMs);
                }
            }

            result.Status = BookStatus.Error;
            result.Message = "Maximum retry attempts reached";
            result.CompletedAt = DateTime.Now;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking climb for {ClimberName}, Event ID: {EventId}", climber.Name, eventId);
            result.Status = BookStatus.Error;
            result.Message = ex.Message;
            result.CompletedAt = DateTime.Now;
            return result;
        }
    }
} 