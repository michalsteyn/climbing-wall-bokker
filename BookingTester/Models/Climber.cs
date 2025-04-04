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

    public int Id { get; set; }

    public Climber()
    {
    }

    public Climber(int id, string name, string email, string password)
    {
        Id = id;
        Name = name;
        Email = email;
        Password = password;
    }
}