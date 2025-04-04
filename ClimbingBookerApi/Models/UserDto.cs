namespace ClimbingBookerApi.Models;

/// <summary>
/// Data Transfer Object for user information.
/// Contains only the essential user details needed for display.
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;
} 