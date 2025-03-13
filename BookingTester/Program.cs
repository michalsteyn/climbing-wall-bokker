using BookingTester;

class Program
{
    static async Task Main(string[] args)
    {

        var climbingBooker = new ClimbingBooker();
        var climbers = new List<Climber>
        {
            new("Michal Steyn", "michalsteyn@gmail.com", "$zLb9g2xML8R5!9CDo2m"),
            new("David Steyn", "michalsteyn+david@gmail.com", "steynfamilyclimbing"),
            new("Zoe Steyn", "michalsteyn+zoe@gmail.com", "steynfamilyclimbing"),
            new("Isaac Steyn", "michalsteyn+isaac@gmail.com", "steynfamilyclimbing"),
            new("Tiffany Steyn", "michalsteyn+tiffany@gmail.com", "steynfamilyclimbing")
        };

        (var climbingEvents, var serverTime) = await climbingBooker.GetClimbingEvents(false);
        var serverLateTime = Math.Max(0, serverTime?.TotalSeconds ?? 0);

        var eventId = 64169121; //64169191
        var climbingEvent = climbingEvents.FirstOrDefault(e => e.Id == eventId);
        
        if (climbingEvent == null)
        {
            UserLogger.Info($"No event found with id: {eventId}");
            return;
        }

        var eventBookableTime = climbingEvent.StartTime - TimeSpan.FromDays(1);
        var adjustedLocalTime = DateTime.Now + TimeSpan.FromSeconds(serverLateTime);

        if (eventBookableTime > adjustedLocalTime)
        {
            var waitTime = eventBookableTime - adjustedLocalTime;
            Console.WriteLine($"Too early to book event, need to wait: {waitTime.TotalSeconds}");
            await Task.Delay(waitTime);
        }

        foreach (var climber in climbers)
        {
            try
            {
                _ = Book(climber, eventId, climbingEvent);
            }
            catch (Exception e)
            {
                UserLogger.Info(climber.Name,
                    $"Error Booking Climb, ID: {eventId}, Date: {climbingEvent.StartTime}, Type: {climbingEvent.Description}");
            }
        }

        UserLogger.Info("Press any key to exit");
        Console.ReadKey();
    }

    public static async Task Book(Climber climber, int eventId, ClimbingEvent climbingEvent)
    {
        UserLogger.Info(climber.Name,
            $"Going to Book Climb, ID: {eventId}, Date: {climbingEvent.StartTime}");//, Type: {climbingEvent.Description}");
        var climbingBooker1 = new ClimbingBooker();
        var result = await climbingBooker1.BookClimb(climber.Name, climber.Email, climber.Password, eventId);
        UserLogger.Info(climber.Name,
            $"Climb Booking, Result: {result}: ID: {eventId}, Date: {climbingEvent.StartTime}");//, Type: {climbingEvent.Description}");
    }
}