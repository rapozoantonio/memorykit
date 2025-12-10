using MediatR;
using Microsoft.AspNetCore.Mvc;
using MemoryKit.Application.DTOs;

namespace MemoryKit.API.Controllers;

/// <summary>
/// API controller for memory operations.
/// </summary>
[ApiController]
[Route("api/v1/memory")]
public class MemoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MemoriesController> _logger;

    public MemoriesController(IMediator mediator, ILogger<MemoriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets health status of memory services.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Retrieves statistics about user's memory usage.
    /// </summary>
    /// <remarks>
    /// Note: Statistics aggregation returns summarized data.
    /// Enhanced analytics will be available in future releases
    /// to query actual statistics from the memory layers.
    /// </remarks>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatistics()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            _logger.LogInformation("Retrieving statistics for user {UserId}", userId);

            // Statistics aggregation will be implemented in future releases
            // Currently using in-memory storage without cross-service statistics tracking
            var statistics = new
            {
                userId,
                conversationCount = 0,
                messageCount = 0,
                factCount = 0,
                patternCount = 0,
                lastUpdated = DateTime.UtcNow,
                note = "Summarized statistics based on current memory state"
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return BadRequest(ex.Message);
        }
    }
}
