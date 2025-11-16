using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MemoryKit.API.Controllers;

/// <summary>
/// API controller for procedural pattern management.
/// </summary>
[ApiController]
[Route("api/v1/patterns")]
public class PatternsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PatternsController> _logger;

    public PatternsController(IMediator mediator, ILogger<PatternsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Lists all procedural patterns for the user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatterns(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            // TODO: Retrieve patterns from IProceduralMemoryService
            return Ok(new[] { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patterns");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a procedural pattern.
    /// </summary>
    [HttpDelete("{patternId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePattern(
        [FromRoute] string patternId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            // TODO: Delete pattern through service
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pattern");
            return BadRequest(ex.Message);
        }
    }
}
