using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BookingTester.Models;

/// <summary>
/// Represents the result of a booking operation.
/// </summary>
public class BookingResult
{
    /// <summary>
    /// The status of the booking operation.
    /// </summary>
    public BookStatus Status { get; set; }

    /// <summary>
    /// The user who attempted the booking.
    /// </summary>
    public UserDto User { get; set; } = null!;

    /// <summary>
    /// The ID of the event that was booked.
    /// </summary>
    public long EventId { get; set; }

    /// <summary>
    /// The number of retry attempts made.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// The timestamp when the booking was completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Any additional message or error information.
    /// </summary>
    public string? Message { get; set; }
} 