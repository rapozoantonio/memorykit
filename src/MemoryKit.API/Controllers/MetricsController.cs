using Microsoft.AspNetCore.Mvc;
using MemoryKit.Infrastructure.Monitoring;

namespace MemoryKit.API.Controllers;

[ApiController]
[Route("api/v1/metrics")]
public class MetricsController : ControllerBase
{
    private readonly PerformanceMetricsCollector _metrics;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(PerformanceMetricsCollector metrics, ILogger<MetricsController> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Gets performance metrics for a specified time window.
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PerformanceSnapshot), StatusCodes.Status200OK)]
    public IActionResult GetPerformanceMetrics([FromQuery] int windowMinutes = 5)
    {
        try
        {
            var snapshot = _metrics.GetSnapshot(TimeSpan.FromMinutes(windowMinutes));
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics" });
        }
    }

    /// <summary>
    /// Gets health status based on performance metrics.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealthMetrics()
    {
        try
        {
            var snapshot = _metrics.GetSnapshot(TimeSpan.FromMinutes(1));
            
            // Determine health based on metrics
            var isHealthy = snapshot.P95LatencyMs < 200; // Target: p95 < 200ms
            var status = isHealthy ? "healthy" : "degraded";

            return Ok(new
            {
                Status = status,
                Metrics = snapshot,
                Thresholds = new
                {
                    P95LatencyMs = 200,
                    ErrorRate = 0.05
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health metrics");
            return StatusCode(500, new { error = "Failed to retrieve health status" });
        }
    }

    /// <summary>
    /// Records a custom metric (for admin purposes).
    /// </summary>
    [HttpPost("record")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult RecordMetric([FromBody] RecordMetricRequest request)
    {
        try
        {
            _metrics.RecordOperation(request.OperationName, request.DurationMs, request.UserId);
            return Ok(new { message = "Metric recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric");
            return BadRequest(new { error = ex.Message });
        }
    }

    public record RecordMetricRequest(string OperationName, long DurationMs, string? UserId = null);
}
