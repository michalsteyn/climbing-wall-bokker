using System.Text.Json.Serialization;

namespace BookingTester.Models;

/// <summary>
/// Data transfer object for user information.
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;
} 