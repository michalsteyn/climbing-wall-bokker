using BookingTester.Models;

namespace BookingTester.Services;

public interface IClimbingBookerClient
{
    Task<(IEnumerable<ClimbingEvent> Events, TimeSpan? ServerTime)> GetClimbingEvents(bool includePastEvents);
    Task<bool> BookClimb(string name, string email, string password, int eventId);
} 