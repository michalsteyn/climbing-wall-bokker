using Newtonsoft.Json;

public class BookingParser
{
    public string GetScheduleData(string bookingSchedule)
    {
        var lines = bookingSchedule.Split("\n");
        foreach (var line in lines)
        {
            if (line.Contains("var app="))
            {
                var eventString = line.Replace("var app=", "");
                return eventString;
            }
        }

        return null;
    }

    public List<ClimbingEvent> ParseBookingSchedule(string bookingSchedule, bool includeCertified)
    {
        var deserialized = JsonConvert.DeserializeObject<List<object[]>>(bookingSchedule);

        // Parse the list of object arrays into a list of ClimbingEvent objects
        var climbingEvents = new List<ClimbingEvent>();
        foreach (var item in deserialized)
        {
            climbingEvents.Add(new ClimbingEvent
            {
                StartTime = UnixTimeStampToDateTime((long)item[0]),
                EndTime = UnixTimeStampToDateTime((long)item[1]),
                Id = (long)item[2],
                Capacity = (long)item[3],
                Booked = (long)item[4],
                SomeProperty3 = (long)item[5],
                SomeProperty4 = (long)item[6],
                Title = (string)item[7],
                Description = (string)item[8],
                SomeProperty5 = (long)item[9],
                SomeProperty6 = (string)item[10],
                SomeProperty7 = (long)item[11]
            });
        }
        var now = DateTime.Now;
        var community = climbingEvents
            .Where(item =>
                item.Title.ToLower().Contains("community") ||
                (includeCertified && item.Title.ToLower().Contains("certified")))
            .Where(item => item.StartTime > now);
        return community.ToList();
    }

    static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp);//.ToLocalTime();
        return dateTime;
    }
}