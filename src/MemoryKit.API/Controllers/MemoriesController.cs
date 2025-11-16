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
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            // TODO: Implement statistics retrieval
            return Ok(new
            {
                userId,
                conversationCount = 0,
                messageCount = 0,
                factCount = 0,
                patternCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return BadRequest(ex.Message);
        }
    }
}
