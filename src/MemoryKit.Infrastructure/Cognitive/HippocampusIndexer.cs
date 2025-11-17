using System.Collections.Concurrent;
using MemoryKit.Domain.Entities;
using MemoryKit.Infrastructure.Azure;

namespace MemoryKit.Infrastructure.Cognitive;

/// <summary>
/// Hippocampus analog: Memory consolidation and indexing.
/// Moves important memories from working memory to long-term storage.
/// </summary>
public class HippocampusIndexer : IHippocampusIndexer
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly IScratchpadService _scratchpad;
    private readonly IEpisodicMemoryService _episodicMemory;
    private readonly IAmygdalaImportanceEngine _amygdala;
    private readonly ILogger<HippocampusIndexer> _logger;
    private readonly ConcurrentDictionary<string, List<string>> _pendingConsolidation = new();

    public HippocampusIndexer(
        IWorkingMemoryService workingMemory,
        IScratchpadService scratchpad,
        IEpisodicMemoryService episodicMemory,
        IAmygdalaImportanceEngine amygdala,
        ILogger<HippocampusIndexer> logger)
    {
        _workingMemory = workingMemory ?? throw new ArgumentNullException(nameof(workingMemory));
        _scratchpad = scratchpad ?? throw new ArgumentNullException(nameof(scratchpad));
        _episodicMemory = episodicMemory ?? throw new ArgumentNullException(nameof(episodicMemory));
        _amygdala = amygdala ?? throw new ArgumentNullException(nameof(amygdala));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> EncodeAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Encoding message {MessageId}", message.Id);

        try
        {
            // Calculate importance
            var importance = await _amygdala.CalculateImportanceAsync(
                message,
                cancellationToken);

            message.Metadata = message.Metadata with
            {
                ImportanceScore = importance.FinalScore
            };

            // Return encoding ID (message ID)
            return message.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encode message {MessageId}", message.Id);
            throw;
        }
    }

    public Task MarkForConsolidationAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Mark message for later consolidation
        var pending = _pendingConsolidation.GetOrAdd("global", _ => new List<string>());
        lock (pending)
        {
            if (!pending.Contains(messageId))
            {
                pending.Add(messageId);
                _logger.LogDebug("Marked message {MessageId} for consolidation", messageId);
            }
        }

        return Task.CompletedTask;
    }

    public async Task ConsolidateAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting global memory consolidation for user {UserId}",
            userId);

        // This would consolidate across all conversations for a user
        // Simplified implementation for in-memory version
        await Task.CompletedTask;
    }

    public async Task ConsolidateAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting memory consolidation for user {UserId}, conversation {ConversationId}",
            userId,
            conversationId);

        try
        {
            // Retrieve recent messages from working memory
            var recentMessages = await _workingMemory.GetRecentAsync(
                userId,
                conversationId,
                count: 50,
                cancellationToken);

            if (!recentMessages.Any())
            {
                _logger.LogDebug("No messages to consolidate");
                return;
            }

            var consolidatedCount = 0;
            var importantMessages = new List<Message>();

            // Identify important messages using amygdala
            foreach (var message in recentMessages)
            {
                var importance = await _amygdala.CalculateImportanceAsync(
                    message,
                    cancellationToken);

                if (importance.FinalScore >= 0.6) // High importance threshold
                {
                    importantMessages.Add(message);
                }
            }

            _logger.LogDebug(
                "Identified {Count} important messages (threshold: 0.6)",
                importantMessages.Count);

            // Consolidate important messages
            foreach (var message in importantMessages)
            {
                // Archive to episodic memory
                await _episodicMemory.ArchiveAsync(message, cancellationToken);
                consolidatedCount++;
            }

            _logger.LogInformation(
                "Memory consolidation complete: {Count} messages consolidated",
                consolidatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory consolidation failed");
            throw;
        }
    }

    public async Task IndexAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Indexing message {MessageId}", message.Id);

        try
        {
            // Calculate importance
            var importance = await _amygdala.CalculateImportanceAsync(
                message,
                cancellationToken);

            message.Metadata = message.Metadata with
            {
                ImportanceScore = importance.FinalScore
            };

            // Archive to episodic memory
            await _episodicMemory.ArchiveAsync(message, cancellationToken);

            _logger.LogDebug(
                "Message indexed with importance score {Score:F3}",
                importance.FinalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index message {MessageId}", message.Id);
            throw;
        }
    }

    public async Task PruneOldMemoriesAsync(
        string userId,
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Pruning memories older than {Days} days for user {UserId}",
            retentionPeriod.TotalDays,
            userId);

        try
        {
            // Prune scratchpad (semantic memory)
            await _scratchpad.PruneAsync(userId, cancellationToken);

            _logger.LogInformation("Memory pruning complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory pruning failed");
            throw;
        }
    }

    public Task<ConsolidationMetrics> GetConsolidationMetricsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Simplified metrics for in-memory implementation
        var metrics = new ConsolidationMetrics
        {
            UserId = userId,
            LastConsolidation = DateTime.UtcNow,
            MessagesConsolidated = 0,
            AverageImportanceScore = 0.5
        };

        return Task.FromResult(metrics);
    }
}

/// <summary>
/// Metrics about memory consolidation.
/// </summary>
public record ConsolidationMetrics
{
    public string UserId { get; init; } = string.Empty;
    public DateTime LastConsolidation { get; init; }
    public int MessagesConsolidated { get; init; }
    public double AverageImportanceScore { get; init; }
}
