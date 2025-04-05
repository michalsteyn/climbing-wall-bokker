using System.Net;
using BookingTester.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace BookingTester.Client;

public class ClimbingBookerClient : IClimbingBooker
{
    private HttpClientHandler handler;
    private HttpClient client;
    private const bool serializeEventsToFile = false;

    public ClimbingBookerClient()
    {
        handler = new HttpClientHandler();
        handler.CookieContainer = new CookieContainer();
        client = new HttpClient(handler);
    }

    public async Task<(List<ClimbingEvent>, TimeSpan?)> GetClimbingEvents(bool includeCertified = false, bool noParse = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.supersaas.com/schedule/Whatcom_Family_YMCA/Climbing_Wall");
        //var request = new HttpRequestMessage(HttpMethod.Get, "https://www.supersaas.com/schedule/login/Whatcom_Family_YMCA/Climbing_Wall?after=%2Fschedule%2FWhatcom_Family_YMCA%2FClimbing_Wall");
        request.Headers.Add("authority", "www.supersaas.com");
        request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        request.Headers.Add("accept-language", "en-US,en;q=0.9");
        request.Headers.Add("cache-control", "no-cache");
        //request.Headers.Add("cookie", "SS_312415_username=michalsteyn%40gmail.comK; _SS_s=czh5TW1KL0tobFdXRDNJL3JjM0V2U1Y0SVRQenVpZWZBSlRSWUZ6OWlpK043SExDRnFROVFNOEFFcXNhNFgrM2lpcm9jcklickQ1VmFLSC8vUjNTbUU5bjMwSEp0WksvNXFxUnNkTmJiQStPbTFBRkhBaTVPeVVRR3ZGdy85R1Uwd21tMFVvdkY5VU1ORGplMDAweDNrWUtING00dFYyTFFoeHhOZHRQLzAzd1NPN3Q2VkNYOExwQi9kVUxPTVZzZVpxTU56bWhWL0lTTWJHakFubUNZaFJRaWlSaHJYaEhjcHIxNDBqU0ZrbkpOOG1qeEphTno5TmpsL1N3MUg1NGNvdTdWaW5KNG9DTWUzdnpXZXpjVlE9PS0tQnlPajBqd25QZ0VXTDZBNUg2azZ1Zz09--dc3c6ab46f1269b7615d606c69d19ec8c019ae47");
        request.Headers.Add("pragma", "no-cache");
        request.Headers.Add("referer", "https://www.supersaas.com/schedule/Whatcom_Family_YMCA/Climbing_Wall");
        request.Headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("sec-fetch-dest", "document");
        request.Headers.Add("sec-fetch-mode", "navigate");
        request.Headers.Add("sec-fetch-site", "same-origin");
        request.Headers.Add("sec-fetch-user", "?1");
        request.Headers.Add("upgrade-insecure-requests", "1");
        request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
        var response = await client.SendAsync(request);
        var serverTimeStamp = response.Headers.Date;
        var serverTimeOffset = serverTimeStamp - DateTime.Now;
        UserLogger.Info($"Server Time Offset: {serverTimeOffset?.TotalSeconds}s");

        response.EnsureSuccessStatusCode();
        var bookingSchedule = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            // Get the cookies from the response and store them in the CookieContainer
            Uri uri = new Uri("https://www.supersaas.com");
            foreach (Cookie cookie in handler.CookieContainer.GetCookies(uri))
            {
                //Console.WriteLine($"Cookie: {cookie.Name}={cookie.Value}");
            }
        }
        else
        {
            UserLogger.Info($"Failed to get index page: {response.StatusCode}");
        }

        if (noParse)
            return (null, null);

        var parser = new BookingParser();
        var schedule = parser.GetScheduleData(bookingSchedule);
        if (schedule == null)
        {
            UserLogger.Info("Failed to get schedule data");
            return (null, null); 
        }
        var climbingEvents = parser.ParseBookingSchedule(schedule, includeCertified);
        if(serializeEventsToFile)
            File.WriteAllText("events.json", JsonConvert.SerializeObject(climbingEvents, Formatting.Indented));
        return (climbingEvents, serverTimeOffset);
    }

    public async Task<bool> LogIn(string name, string user, string pass)
    {
        if (handler.CookieContainer.Count == 0)
        {
            UserLogger.Info(name, "Missing Cookies, Going to Log In First");
            await GetClimbingEvents(noParse: true);
        }

        //var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.supersaas.com/schedule/login/Whatcom_Family_YMCA/Climbing_Wall?after=%2Fschedule%2FWhatcom_Family_YMCA%2FClimbing_Wall");
        request.Headers.Add("authority", "www.supersaas.com");
        request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        request.Headers.Add("accept-language", "en-US,en;q=0.9");
        request.Headers.Add("cache-control", "no-cache");
        //request.Headers.Add("content-type", "application/x-www-form-urlencoded");
        //request.Headers.Add("cookie", "SS_312415_username=michalsteyn%40gmail.comK; _SS_s=ZDNycDY0TXAyMkZvVEZCMzgxYjFwNjZDc2NNaGpLcThUSGlhMVljdExaTllqVnFiaUhqSUg2UkNxVXVFazFHQVF4eGVURXpMamVXZTNpRkt6VXUyTHZiSVpWVGxDQ3hRc1BOTmxGdnloNWVUaS9JVkFlV0R2ZjV4MFJJeU14UU5FM0NOamlHNEJtWmUvWWdTT25ZMC8rdk5jU1gwdVRpeWpTendKVzRqSTRqRUk3anlzelgyVkpJSHg3QyszdWZNaXNrN2x5TUVZTTZrRE1ZK25WaXNzTktiazZaa011ZHVoOVFzMzZHZE1jcys2QXAvWlM5REJGY3MzQWlqclVHamZiWHVwTldCc1VjNUxGOUJOdWUvelE9PS0tOUMxeVc4aEtBSmR4enVvWlNmaFNRUT09--432a3350c4ada1d3f9e41f0024d905b715de8080");
        request.Headers.Add("origin", "https://www.supersaas.com");
        request.Headers.Add("pragma", "no-cache");
        request.Headers.Add("referer", "https://www.supersaas.com/schedule/login/Whatcom_Family_YMCA/Climbing_Wall?after=%2Fschedule%2FWhatcom_Family_YMCA%2FClimbing_Wall");
        request.Headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("sec-fetch-dest", "document");
        request.Headers.Add("sec-fetch-mode", "navigate");
        request.Headers.Add("sec-fetch-site", "same-origin");
        request.Headers.Add("sec-fetch-user", "?1");
        request.Headers.Add("upgrade-insecure-requests", "1");
        request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
        var collection = new List<KeyValuePair<string, string>>
        {
            new("name", user),
            new("password", pass),
            new("remember", "K"),
            new("button", "")
        };
        var content = new FormUrlEncodedContent(collection);
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();
        if (responseData.Contains($"Signed in as {user}"))
        {
            UserLogger.Info(name, "Logged In");
            return true;
        }

        UserLogger.Info(name, "Failed to Log In");
        return false;
    }

    public async Task<BookStatus> BookClimb(long eventId, string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.supersaas.com/schedule/Whatcom_Family_YMCA/Climbing_Wall?view=week&day=20&month=2");
        request.Headers.Add("authority", "www.supersaas.com");
        request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        request.Headers.Add("accept-language", "en-US,en;q=0.9");
        request.Headers.Add("cache-control", "no-cache");
        //request.Headers.Add("content-type", "application/x-www-form-urlencoded");
        //request.Headers.Add("cookie", "SS_312415_keep=ad5bf72ba9bb2a28; _SS_s=Z3FoU0dpejhRU0pEYUwrdXY2QVcwajdwMjRmK243R1BQUTQ1L3NIdGtBNkE3S3lZTFJ3UG5SU2lZTmNYd2V2M2VrdWdXNnFXNmZ6MHdSRVVvUUpvSnNVaDd3ZVI1UWdvRGJzY1kyU29vaTlaY0ZZYlFhQjhmTDZpWUZrZERVYWcxNzdnTHQvMTJUMm5hbHlaWmhGenZod2tUWExFMEZLK0xncFg5OGZMTnMxZEpiTUtKRFI1dUpPR2hjSHlhc3VELzBTd2xpNlZ6cnpIN2tiNW1SMTNpN2l1ejFFbFZCQzFBQXJnNGVOTG5LQlhHNkkzLzlVT2RDSiszTlJXUlViTHVWcEk0ODdzSUlORitsYVYzYll4d3hNQUUzOXc3T2JFVEdhb3RPc0V6NGRwanRBTGdJcVBXZEJpRXIyM3o3VjlEMGUvOHJqMU1lSEpyTVdwTUo5ZXpxZFdaQnZTekFzSGU4N3lYQTlNOVhQWTR3VCtMd3cvNmg5enBjKzIwSXRJSUl1MURyREcvUlI5bENTR3U3VGIyVUpVQTVhQnUwdXBZcjVFYlVXYUZsdz0tLVNLSzBpUWgwMEN2cWIwNHppTm9vQnc9PQ%3D%3D--1253b43d0e28f5fcff92190910cebffa1a7a9d34");
        request.Headers.Add("origin", "https://www.supersaas.com");
        request.Headers.Add("pragma", "no-cache");
        request.Headers.Add("referer", "https://www.supersaas.com/schedule/Whatcom_Family_YMCA/Climbing_Wall?view=week&day=19&month=2");
        request.Headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("sec-fetch-dest", "document");
        request.Headers.Add("sec-fetch-mode", "navigate");
        request.Headers.Add("sec-fetch-site", "same-origin");
        request.Headers.Add("sec-fetch-user", "?1");
        request.Headers.Add("upgrade-insecure-requests", "1");
        request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
        var collection = new List<KeyValuePair<string, string>>
        {
            new("booking[full_name]", name),
            new("booking[confirm]", "0"),
            new("booking[slot_id]", eventId.ToString()),
            new("button", "")
        };
        //collection.Add(new("booking[xpos]", "342"));
        //collection.Add(new("booking[ypos]", "1018"));
        var content = new FormUrlEncodedContent(collection);
        request.Content = content;
        var response = await client.SendAsync(request);
        //response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        var tooEarly = responseString.Contains("Reservations cannot be made more than 24 hours in advance");
        
        if (tooEarly)
            return BookStatus.TooEarly;

        var alreadyBooked = responseString.Contains("You cannot put more than 1 reservation on the same day") ||
                            responseString.Contains("Only one reservation allowed per slot per Members");
        if (alreadyBooked)
            return BookStatus.AlreadyBooked;

        //Console.WriteLine(responseString);
        return response.StatusCode == HttpStatusCode.NotFound ? BookStatus.OK : BookStatus.Error;
    }

    public async Task<BookStatus> CheckBooking(long eventId, string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.supersaas.com/schedule/Whatcom_Family_YMCA/Climbing_Wall?view=agenda");
        request.Headers.Add("authority", "www.supersaas.com");
        request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        request.Headers.Add("accept-language", "en-US,en;q=0.9");
        request.Headers.Add("cache-control", "no-cache");
        //request.Headers.Add("content-type", "application/x-www-form-urlencoded");
        //request.Headers.Add("cookie", "SS_312415_keep=ad5bf72ba9bb2a28; _SS_s=Z3FoU0dpejhRU0pEYUwrdXY2QVcwajdwMjRmK243R1BQUTQ1L3NIdGtBNkE3S3lZTFJ3UG5SU2lZTmNYd2V2M2VrdWdXNnFXNmZ6MHdSRVVvUUpvSnNVaDd3ZVI1UWdvRGJzY1kyU29vaTlaY0ZZYlFhQjhmTDZpWUZrZERVYWcxNzdnTHQvMTJUMm5hbHlaWmhGenZod2tUWExFMEZLK0xncFg5OGZMTnMxZEpiTUtKRFI1dUpPR2hjSHlhc3VELzBTd2xpNlZ6cnpIN2tiNW1SMTNpN2l1ejFFbFZCQzFBQXJnNGVOTG5LQlhHNkkzLzlVT2RDSiszTlJXUlViTHVWcEk0ODdzSUlORitsYVYzYll4d3hNQUUzOXc3T2JFVEdhb3RPc0V6NGRwanRBTGdJcVBXZEJpRXIyM3o3VjlEMGUvOHJqMU1lSEpyTVdwTUo5ZXpxZFdaQnZTekFzSGU4N3lYQTlNOVhQWTR3VCtMd3cvNmg5enBjKzIwSXRJSUl1MURyREcvUlI5bENTR3U3VGIyVUpVQTVhQnUwdXBZcjVFYlVXYUZsdz0tLVNLSzBpUWgwMEN2cWIwNHppTm9vQnc9PQ%3D%3D--1253b43d0e28f5fcff92190910cebffa1a7a9d34");
        request.Headers.Add("origin", "https://www.supersaas.com");
        request.Headers.Add("pragma", "no-cache");
        request.Headers.Add("referer", "https://www.supersaas.com/schedule/Whatcom_Family_YMCA/Climbing_Wall?view=week&day=19&month=2");
        request.Headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("sec-fetch-dest", "document");
        request.Headers.Add("sec-fetch-mode", "navigate");
        request.Headers.Add("sec-fetch-site", "same-origin");
        request.Headers.Add("sec-fetch-user", "?1");
        request.Headers.Add("upgrade-insecure-requests", "1");
        request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");

        var response = await client.SendAsync(request);
        //response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(responseString);
        // Find the tr element anywhere in the document
        var trElements = doc.DocumentNode.SelectNodes("//tr");

        if (trElements != null)
        {
            foreach (var tr in trElements)
            {
                string trId = tr.GetAttributeValue("id", "");
                if (trId == $"c{eventId}") // Check if this is the correct ID
                {
                    UserLogger.Info(name, $"Found <tr> element with ID {trId}");

                    // Now, check if this <tr> contains a <span> with class "wl pad"
                    var spanElement = tr.SelectSingleNode(".//span[contains(@class, 'wl') and contains(@class, 'pad')]");
                    if (spanElement != null)
                    {
                        UserLogger.Info(name, "Detected Waitlist");
                        return BookStatus.Waitlisted;
                    }

                    return BookStatus.OK;
                }
            }
        }
        UserLogger.Info(name, $"Didn't find Booking for even: {eventId}");
        return BookStatus.Error;
    }

    public async Task<BookStatus> BookClimb(string name, string user, string pass, long eventId)
    {
        if (!await LogIn(name, user, pass))
            return BookStatus.Error;

        var result = await BookClimb(eventId, name);
        UserLogger.Info(name, $"Booking Result, Status: {result}");
        
        // For AlreadyBooked status, verify the booking
        if (result == BookStatus.AlreadyBooked)
        {
            var bookingCheck = await CheckBooking(eventId, name);
            return bookingCheck;
        }

        return result;
    }

    
}