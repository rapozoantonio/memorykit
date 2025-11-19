using MediatR;
using MemoryKit.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MemoryKit.API.Controllers;

/// <summary>
/// API controller for procedural pattern management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class PatternsController : ControllerBase
{
    private readonly IProceduralMemoryService _proceduralMemory;
    private readonly ILogger<PatternsController> _logger;

    public PatternsController(
        IProceduralMemoryService proceduralMemory,
        ILogger<PatternsController> logger)
    {
        _proceduralMemory = proceduralMemory ?? throw new ArgumentNullException(nameof(proceduralMemory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all procedural patterns for the user with analytics.
    /// </summary>
    /// <param name="userId">User ID (defaults to 'demo' for MVP)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(PatternAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatterns(
        [FromQuery] string userId = "demo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving patterns for user {UserId}", userId);

            var patterns = await _proceduralMemory.GetUserPatternsAsync(userId, cancellationToken);

            var response = new PatternAnalyticsResponse
            {
                UserId = userId,
                TotalPatterns = patterns.Length,
                Patterns = patterns.Select(p => new PatternDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    InstructionTemplate = p.InstructionTemplate,
                    UsageCount = p.UsageCount,
                    LastUsed = p.LastUsed,
                    CreatedAt = p.CreatedAt,
                    ConfidenceThreshold = p.ConfidenceThreshold,
                    Triggers = p.Triggers.Select(t => new TriggerDto
                    {
                        Type = t.Type.ToString(),
                        Pattern = t.Pattern
                    }).ToArray()
                }).ToArray(),
                MostUsedPattern = patterns.OrderByDescending(p => p.UsageCount).FirstOrDefault()?.Name,
                AverageUsageCount = patterns.Any() ? patterns.Average(p => p.UsageCount) : 0
            };

            _logger.LogInformation("Retrieved {Count} patterns for user {UserId}", patterns.Length, userId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patterns for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to retrieve patterns",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets a specific pattern by ID.
    /// </summary>
    [HttpGet("{patternId}")]
    [ProducesResponseType(typeof(PatternDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPattern(
        [FromRoute] string patternId,
        [FromQuery] string userId = "demo",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patterns = await _proceduralMemory.GetUserPatternsAsync(userId, cancellationToken);
            var pattern = patterns.FirstOrDefault(p => p.Id == patternId);

            if (pattern == null)
            {
                return NotFound(new { error = $"Pattern {patternId} not found" });
            }

            var dto = new PatternDto
            {
                Id = pattern.Id,
                Name = pattern.Name,
                Description = pattern.Description,
                InstructionTemplate = pattern.InstructionTemplate,
                UsageCount = pattern.UsageCount,
                LastUsed = pattern.LastUsed,
                CreatedAt = pattern.CreatedAt,
                ConfidenceThreshold = pattern.ConfidenceThreshold,
                Triggers = pattern.Triggers.Select(t => new TriggerDto
                {
                    Type = t.Type.ToString(),
                    Pattern = t.Pattern
                }).ToArray()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pattern {PatternId}", patternId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to retrieve pattern",
                message = ex.Message
            });
        }
    }
}

/// <summary>
/// Response containing pattern analytics.
/// </summary>
public record PatternAnalyticsResponse
{
    public required string UserId { get; init; }
    public int TotalPatterns { get; init; }
    public required PatternDto[] Patterns { get; init; }
    public string? MostUsedPattern { get; init; }
    public double AverageUsageCount { get; init; }
}

/// <summary>
/// DTO for a procedural pattern.
/// </summary>
public record PatternDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string InstructionTemplate { get; init; }
    public int UsageCount { get; init; }
    public DateTime LastUsed { get; init; }
    public DateTime CreatedAt { get; init; }
    public double ConfidenceThreshold { get; init; }
    public required TriggerDto[] Triggers { get; init; }
}

/// <summary>
/// DTO for a pattern trigger.
/// </summary>
public record TriggerDto
{
    public required string Type { get; init; }
    public required string Pattern { get; init; }
}
