using BookingTester.Models;
using Microsoft.Extensions.Logging;

namespace BookingTester.Services;

public interface IBookingService
{
    Task<BookStatus> BookClimberAsync(Climber climber, long eventId);
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
} 