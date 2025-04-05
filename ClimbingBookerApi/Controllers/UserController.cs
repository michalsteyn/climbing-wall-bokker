using BookingTester.Models;
using BookingTester.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClimbingBookerApi.Controllers;

/// <summary>
/// Controller for managing climber information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
public class UserController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserManager userManager, ILogger<UserController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all registered climbers with basic information.
    /// </summary>
    /// <returns>A list of climbers with their ID and name.</returns>
    /// <response code="200">Returns the list of climbers.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetClimbers()
    {
        _logger.LogInformation("Retrieving all climbers");
        var climbers = await _userManager.GetClimbersAsync();
        var userDtos = climbers.Select(c => new UserDto
        {
            Id = c.Id,
            Name = c.Name
        });
        return Ok(userDtos);
    }
} 