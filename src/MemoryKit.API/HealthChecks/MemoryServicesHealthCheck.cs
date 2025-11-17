using Microsoft.Extensions.Diagnostics.HealthChecks;
using MemoryKit.Infrastructure.Azure;

namespace MemoryKit.API.HealthChecks;

/// <summary>
/// Health check for memory layer services.
/// </summary>
public class MemoryServicesHealthCheck : IHealthCheck
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly IScratchpadService _scratchpad;
    private readonly IEpisodicMemoryService _episodicMemory;
    private readonly IProceduralMemoryService _proceduralMemory;
    private readonly ILogger<MemoryServicesHealthCheck> _logger;

    public MemoryServicesHealthCheck(
        IWorkingMemoryService workingMemory,
        IScratchpadService scratchpad,
        IEpisodicMemoryService episodicMemory,
        IProceduralMemoryService proceduralMemory,
        ILogger<MemoryServicesHealthCheck> logger)
    {
        _workingMemory = workingMemory ?? throw new ArgumentNullException(nameof(workingMemory));
        _scratchpad = scratchpad ?? throw new ArgumentNullException(nameof(scratchpad));
        _episodicMemory = episodicMemory ?? throw new ArgumentNullException(nameof(episodicMemory));
        _proceduralMemory = proceduralMemory ?? throw new ArgumentNullException(nameof(proceduralMemory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();
            var healthChecks = new List<(string Name, bool Healthy, string? Error)>();

            // Check Working Memory
            try
            {
                var testMessages = await _workingMemory.GetRecentAsync("healthcheck", "test", 1, cancellationToken);
                healthChecks.Add(("WorkingMemory", true, null));
                data["workingMemory"] = "healthy";
            }
            catch (Exception ex)
            {
                healthChecks.Add(("WorkingMemory", false, ex.Message));
                data["workingMemory"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Working memory health check failed");
            }

            // Check Scratchpad
            try
            {
                var testFacts = await _scratchpad.SearchFactsAsync("healthcheck", "test", 1, cancellationToken);
                healthChecks.Add(("Scratchpad", true, null));
                data["scratchpad"] = "healthy";
            }
            catch (Exception ex)
            {
                healthChecks.Add(("Scratchpad", false, ex.Message));
                data["scratchpad"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Scratchpad health check failed");
            }

            // Check Episodic Memory
            try
            {
                var testArchive = await _episodicMemory.SearchAsync("healthcheck", "test", 1, cancellationToken);
                healthChecks.Add(("EpisodicMemory", true, null));
                data["episodicMemory"] = "healthy";
            }
            catch (Exception ex)
            {
                healthChecks.Add(("EpisodicMemory", false, ex.Message));
                data["episodicMemory"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Episodic memory health check failed");
            }

            // Check Procedural Memory
            try
            {
                var testPatterns = await _proceduralMemory.GetUserPatternsAsync("healthcheck", cancellationToken);
                healthChecks.Add(("ProceduralMemory", true, null));
                data["proceduralMemory"] = "healthy";
            }
            catch (Exception ex)
            {
                healthChecks.Add(("ProceduralMemory", false, ex.Message));
                data["proceduralMemory"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Procedural memory health check failed");
            }

            // Determine overall health
            var allHealthy = healthChecks.All(h => h.Healthy);
            var anyHealthy = healthChecks.Any(h => h.Healthy);

            if (allHealthy)
            {
                return HealthCheckResult.Healthy("All memory services are operational", data);
            }
            else if (anyHealthy)
            {
                var unhealthyServices = string.Join(", ", healthChecks.Where(h => !h.Healthy).Select(h => h.Name));
                return HealthCheckResult.Degraded($"Some memory services are unavailable: {unhealthyServices}", data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy("All memory services are unavailable", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during memory services health check");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
