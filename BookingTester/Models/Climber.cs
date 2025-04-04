using System.Text.Json.Serialization;

namespace BookingTester.Models;

public class Climber
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    public Climber(string name, string email, string password)
    {
        Name = name;
        Email = email;
        Password = password;
    }
}