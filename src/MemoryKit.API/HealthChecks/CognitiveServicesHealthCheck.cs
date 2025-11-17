using Microsoft.Extensions.Diagnostics.HealthChecks;
using MemoryKit.Infrastructure.Cognitive;
using MemoryKit.Domain.Interfaces;

namespace MemoryKit.API.HealthChecks;

/// <summary>
/// Health check for cognitive services (Prefrontal, Amygdala, Hippocampus).
/// </summary>
public class CognitiveServicesHealthCheck : IHealthCheck
{
    private readonly IPrefrontalController _prefrontal;
    private readonly IAmygdalaImportanceEngine _amygdala;
    private readonly IHippocampusIndexer _hippocampus;
    private readonly IMemoryOrchestrator _orchestrator;
    private readonly ILogger<CognitiveServicesHealthCheck> _logger;

    public CognitiveServicesHealthCheck(
        IPrefrontalController prefrontal,
        IAmygdalaImportanceEngine amygdala,
        IHippocampusIndexer hippocampus,
        IMemoryOrchestrator orchestrator,
        ILogger<CognitiveServicesHealthCheck> logger)
    {
        _prefrontal = prefrontal ?? throw new ArgumentNullException(nameof(prefrontal));
        _amygdala = amygdala ?? throw new ArgumentNullException(nameof(amygdala));
        _hippocampus = hippocampus ?? throw new ArgumentNullException(nameof(hippocampus));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();
            var healthChecks = new List<(string Name, bool Healthy)>();

            // Check Prefrontal Controller
            try
            {
                var testPlan = await _prefrontal.BuildQueryPlanAsync(
                    "test query",
                    new MemoryKit.Domain.ValueObjects.ConversationState
                    {
                        ConversationId = "healthcheck",
                        MessageCount = 0,
                        LastActivity = DateTime.UtcNow
                    },
                    cancellationToken);

                healthChecks.Add(("PrefrontalController", true));
                data["prefrontalController"] = "healthy";
            }
            catch (Exception ex)
            {
                healthChecks.Add(("PrefrontalController", false));
                data["prefrontalController"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Prefrontal controller health check failed");
            }

            // Check Amygdala Importance Engine
            try
            {
                var testMessage = MemoryKit.Domain.Entities.Message.Create(
                    "healthcheck",
                    "test",
                    MemoryKit.Domain.Enums.MessageRole.User,
                    "test message");

                var importance = await _amygdala.CalculateImportanceAsync(testMessage, cancellationToken);

                healthChecks.Add(("AmygdalaImportanceEngine", true));
                data["amygdalaImportanceEngine"] = "healthy";
            }
            catch (Exception ex)
            {
                healthChecks.Add(("AmygdalaImportanceEngine", false));
                data["amygdalaImportanceEngine"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Amygdala importance engine health check failed");
            }

            // Check Hippocampus Indexer (basic verification)
            try
            {
                // Hippocampus is more of a background service, so we just verify it's instantiated
                if (_hippocampus != null)
                {
                    healthChecks.Add(("HippocampusIndexer", true));
                    data["hippocampusIndexer"] = "healthy";
                }
                else
                {
                    healthChecks.Add(("HippocampusIndexer", false));
                    data["hippocampusIndexer"] = "unhealthy: service not initialized";
                }
            }
            catch (Exception ex)
            {
                healthChecks.Add(("HippocampusIndexer", false));
                data["hippocampusIndexer"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Hippocampus indexer health check failed");
            }

            // Check Memory Orchestrator
            try
            {
                // Just verify it's instantiated - actual functionality is tested by integration tests
                if (_orchestrator != null)
                {
                    healthChecks.Add(("MemoryOrchestrator", true));
                    data["memoryOrchestrator"] = "healthy";
                }
                else
                {
                    healthChecks.Add(("MemoryOrchestrator", false));
                    data["memoryOrchestrator"] = "unhealthy: service not initialized";
                }
            }
            catch (Exception ex)
            {
                healthChecks.Add(("MemoryOrchestrator", false));
                data["memoryOrchestrator"] = $"unhealthy: {ex.Message}";
                _logger.LogError(ex, "Memory orchestrator health check failed");
            }

            // Determine overall health
            var allHealthy = healthChecks.All(h => h.Healthy);
            var anyHealthy = healthChecks.Any(h => h.Healthy);

            if (allHealthy)
            {
                return HealthCheckResult.Healthy("All cognitive services are operational", data);
            }
            else if (anyHealthy)
            {
                var unhealthyServices = string.Join(", ", healthChecks.Where(h => !h.Healthy).Select(h => h.Name));
                return HealthCheckResult.Degraded($"Some cognitive services are unavailable: {unhealthyServices}", data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy("All cognitive services are unavailable", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during cognitive services health check");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
