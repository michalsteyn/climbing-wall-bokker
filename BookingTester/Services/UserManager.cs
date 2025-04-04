using System.Text.Json;
using Microsoft.Extensions.Logging;
using BookingTester.Models;

namespace BookingTester.Services;

public interface IUserManager
{
    Task<IEnumerable<Climber>> GetClimbersAsync();
    Task SaveClimbersAsync(IEnumerable<Climber> climbers);
}

public class UserManager : IUserManager
{
    private const string UsersConfigFile = "users.json";
    private readonly ILogger<UserManager> _logger;

    public UserManager(ILogger<UserManager> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<Climber>> GetClimbersAsync()
    {
        try
        {
            if (!File.Exists(UsersConfigFile))
            {
                _logger.LogWarning("Users configuration file not found. Creating default configuration.");
                var defaultClimbers = new List<Climber>
                {
                    new("David Steyn", "michalsteyn+david@gmail.com", "steynfamilyclimbing"),
                    new("Zoe Steyn", "michalsteyn+zoe@gmail.com", "steynfamilyclimbing"),
                    new("Isaac Steyn", "michalsteyn+isaac@gmail.com", "steynfamilyclimbing"),
                    new("Tiffany Steyn", "michalsteyn+tiffany@gmail.com", "steynfamilyclimbing")
                };
                await SaveClimbersAsync(defaultClimbers);
                return defaultClimbers;
            }

            var json = await File.ReadAllTextAsync(UsersConfigFile);
            return JsonSerializer.Deserialize<List<Climber>>(json) ?? new List<Climber>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading climbers configuration");
            throw;
        }
    }

    public async Task SaveClimbersAsync(IEnumerable<Climber> climbers)
    {
        try
        {
            var json = JsonSerializer.Serialize(climbers, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(UsersConfigFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving climbers configuration");
            throw;
        }
    }
} 