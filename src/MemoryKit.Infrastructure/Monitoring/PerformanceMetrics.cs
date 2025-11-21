using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Monitoring;

/// <summary>
/// Collects and aggregates performance metrics for monitoring dashboards.
/// </summary>
public class PerformanceMetricsCollector
{
    private readonly ConcurrentQueue<MetricEntry> _metrics = new();
    private readonly ILogger<PerformanceMetricsCollector> _logger;
    private const int MaxQueueSize = 10000;

    public PerformanceMetricsCollector(ILogger<PerformanceMetricsCollector> logger)
    {
        _logger = logger;
    }

    public void RecordOperation(string operationName, long durationMs, string? userId = null)
    {
        var metric = new MetricEntry
        {
            Timestamp = DateTime.UtcNow,
            OperationName = operationName,
            DurationMs = durationMs,
            UserId = userId
        };

        _metrics.Enqueue(metric);

        // Prevent unbounded growth
        while (_metrics.Count > MaxQueueSize)
        {
            _metrics.TryDequeue(out _);
        }
    }

    public PerformanceSnapshot GetSnapshot(TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        var recent = _metrics.Where(m => m.Timestamp >= cutoff).ToList();

        if (!recent.Any())
        {
            return new PerformanceSnapshot
            {
                Window = window,
                TotalOperations = 0,
                AverageLatencyMs = 0,
                P50LatencyMs = 0,
                P95LatencyMs = 0,
                P99LatencyMs = 0,
                OperationsPerSecond = 0
            };
        }

        var latencies = recent.Select(m => m.DurationMs).OrderBy(x => x).ToList();
        var opsPerSec = recent.Count / window.TotalSeconds;

        return new PerformanceSnapshot
        {
            Window = window,
            TotalOperations = recent.Count,
            AverageLatencyMs = recent.Average(m => m.DurationMs),
            P50LatencyMs = GetPercentile(latencies, 0.50),
            P95LatencyMs = GetPercentile(latencies, 0.95),
            P99LatencyMs = GetPercentile(latencies, 0.99),
            OperationsPerSecond = opsPerSec,
            ByOperation = recent.GroupBy(m => m.OperationName)
                .ToDictionary(
                    g => g.Key,
                    g => new OperationStats
                    {
                        Count = g.Count(),
                        AverageMs = g.Average(m => m.DurationMs),
                        MinMs = g.Min(m => m.DurationMs),
                        MaxMs = g.Max(m => m.DurationMs)
                    })
        };
    }

    private static long GetPercentile(List<long> sortedValues, double percentile)
    {
        if (!sortedValues.Any()) return 0;
        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }

    private class MetricEntry
    {
        public DateTime Timestamp { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public string? UserId { get; set; }
    }
}

public record PerformanceSnapshot
{
    public TimeSpan Window { get; init; }
    public int TotalOperations { get; init; }
    public double AverageLatencyMs { get; init; }
    public long P50LatencyMs { get; init; }
    public long P95LatencyMs { get; init; }
    public long P99LatencyMs { get; init; }
    public double OperationsPerSecond { get; init; }
    public Dictionary<string, OperationStats> ByOperation { get; init; } = new();
}

public record OperationStats
{
    public int Count { get; init; }
    public double AverageMs { get; init; }
    public long MinMs { get; init; }
    public long MaxMs { get; init; }
}
