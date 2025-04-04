public interface IClimbingBooker
{
    Task<(List<ClimbingEvent>, TimeSpan?)> GetClimbingEvents(bool includeCertified = false, bool noParse = false);
    Task<bool> LogIn(string name, string user, string pass);
    Task<BookStatus> BookClimb(long eventId, string name);
    Task<BookStatus> BookClimb(string name, string user, string pass, long eventId);
    Task<BookStatus> CheckBooking(long eventId, string name);
}