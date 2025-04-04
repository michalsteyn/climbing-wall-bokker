using BookingTester.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClimbingBookerApi.Controllers;

/// <summary>
/// Controller for managing climbing events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
public class EventController : ControllerBase
{
    private readonly IEventManager _eventManager;
    private readonly ILogger<EventController> _logger;

    public EventController(IEventManager eventManager, ILogger<EventController> logger)
    {
        _eventManager = eventManager;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the next available climbing event.
    /// </summary>
    /// <returns>The next climbing event, if available.</returns>
    /// <response code="200">Returns the next climbing event.</response>
    /// <response code="404">If no next event is available.</response>
    [HttpGet("next")]
    [ProducesResponseType(typeof(ClimbingEvent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClimbingEvent>> GetNextEvent()
    {
        _logger.LogInformation("Retrieving next climbing event");
        var nextEvent = await _eventManager.FindNextAvailableEventAsync();
        if (nextEvent == null)
        {
            return NotFound("No upcoming events found");
        }
        return Ok(nextEvent);
    }

    /// <summary>
    /// Retrieves all climbing events.
    /// </summary>
    /// <returns>A list of all climbing events.</returns>
    /// <response code="200">Returns the list of climbing events.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClimbingEvent>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClimbingEvent>>> GetEvents()
    {
        _logger.LogInformation("Retrieving all climbing events");
        var events = await _eventManager.GetAllEventsAsync();
        return Ok(events);
    }
} 