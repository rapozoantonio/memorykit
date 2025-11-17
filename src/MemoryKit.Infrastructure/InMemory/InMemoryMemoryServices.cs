using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.InMemory;

/// <summary>
/// In-memory Working Memory Service implementation for MVP/testing.
/// Implements Layer 3 (L3) - Hot context for active conversations.
/// </summary>
public class InMemoryWorkingMemoryService : IWorkingMemoryService
{
    private readonly Dictionary<string, List<Message>> _storage = new();
    private readonly object _lock = new();
    private const int MaxItems = 10;
    private readonly ILogger<InMemoryWorkingMemoryService> _logger;

    public InMemoryWorkingMemoryService(ILogger<InMemoryWorkingMemoryService> logger)
    {
        _logger = logger;
    }

    public async Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Simulate async

        lock (_lock)
        {
            var key = GetKey(userId, conversationId);

            if (!_storage.ContainsKey(key))
            {
                _storage[key] = new List<Message>();
            }

            _storage[key].Add(message);

            // Keep only most recent/important items
            if (_storage[key].Count > MaxItems)
            {
                // Remove least important oldest message
                var toRemove = _storage[key]
                    .OrderBy(m => m.Metadata.ImportanceScore)
                    .ThenBy(m => m.Timestamp)
                    .First();

                _storage[key].Remove(toRemove);
            }

            _logger.LogDebug(
                "Added message to working memory: {ConversationId}, Total: {Count}",
                conversationId,
                _storage[key].Count);
        }
    }

    public async Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            var key = GetKey(userId, conversationId);

            if (!_storage.ContainsKey(key))
            {
                return Array.Empty<Message>();
            }

            return _storage[key]
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToArray();
        }
    }

    public async Task ClearAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            var key = GetKey(userId, conversationId);
            _storage.Remove(key);

            _logger.LogDebug("Cleared working memory for conversation: {ConversationId}", conversationId);
        }
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            var keysToRemove = _storage.Keys
                .Where(k => k.StartsWith($"{userId}:"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _storage.Remove(key);
            }

            _logger.LogInformation(
                "Deleted all working memory data for user {UserId}: {Count} conversations removed",
                userId,
                keysToRemove.Count);
        }
    }

    private static string GetKey(string userId, string conversationId)
        => $"{userId}:{conversationId}";
}

/// <summary>
/// In-memory Scratchpad Service implementation for MVP/testing.
/// Implements Layer 2 (L2) - Semantic memory with extracted facts.
/// </summary>
public class InMemoryScratchpadService : IScratchpadService
{
    private readonly Dictionary<string, List<ExtractedFact>> _storage = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryScratchpadService> _logger;

    public InMemoryScratchpadService(ILogger<InMemoryScratchpadService> logger)
    {
        _logger = logger;
    }

    public async Task StoreFactsAsync(
        string userId,
        string conversationId,
        ExtractedFact[] facts,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (!_storage.ContainsKey(userId))
            {
                _storage[userId] = new List<ExtractedFact>();
            }

            _storage[userId].AddRange(facts);

            _logger.LogDebug(
                "Stored {Count} facts for user {UserId}",
                facts.Length,
                userId);
        }
    }

    public async Task<ExtractedFact[]> SearchFactsAsync(
        string userId,
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (!_storage.ContainsKey(userId))
            {
                return Array.Empty<ExtractedFact>();
            }

            var queryLower = query.ToLowerInvariant();

            // Simple keyword matching (in production, use vector similarity)
            var results = _storage[userId]
                .Where(f =>
                    f.Key.Contains(queryLower, StringComparison.OrdinalIgnoreCase) ||
                    f.Value.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Importance)
                .ThenByDescending(f => f.LastAccessed)
                .Take(maxResults)
                .ToArray();

            _logger.LogDebug(
                "Found {Count} facts for query: {Query}",
                results.Length,
                query);

            return results;
        }
    }

    public async Task RecordAccessAsync(string factId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            foreach (var userFacts in _storage.Values)
            {
                var fact = userFacts.FirstOrDefault(f => f.Id == factId);
                if (fact != null)
                {
                    fact.RecordAccess();
                    break;
                }
            }
        }
    }

    public async Task PruneAsync(string userId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (!_storage.ContainsKey(userId))
            {
                return;
            }

            var ttl = TimeSpan.FromDays(30);
            var minAccessCount = 2;

            var beforeCount = _storage[userId].Count;

            _storage[userId].RemoveAll(f => f.ShouldEvict(ttl, minAccessCount));

            var afterCount = _storage[userId].Count;

            _logger.LogInformation(
                "Pruned {Count} facts for user {UserId}",
                beforeCount - afterCount,
                userId);
        }
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (_storage.ContainsKey(userId))
            {
                var count = _storage[userId].Count;
                _storage.Remove(userId);

                _logger.LogInformation(
                    "Deleted all semantic memory data for user {UserId}: {Count} facts removed",
                    userId,
                    count);
            }
            else
            {
                _logger.LogDebug("No semantic memory data found for user {UserId}", userId);
            }
        }
    }
}

/// <summary>
/// In-memory Episodic Memory Service implementation for MVP/testing.
/// Implements Layer 1 (L1) - Full conversation archive.
/// </summary>
public class InMemoryEpisodicMemoryService : IEpisodicMemoryService
{
    private readonly Dictionary<string, Message> _messagesById = new();
    private readonly Dictionary<string, List<Message>> _messagesByUser = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryEpisodicMemoryService> _logger;

    public InMemoryEpisodicMemoryService(ILogger<InMemoryEpisodicMemoryService> logger)
    {
        _logger = logger;
    }

    public async Task ArchiveAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            _messagesById[message.Id] = message;

            if (!_messagesByUser.ContainsKey(message.UserId))
            {
                _messagesByUser[message.UserId] = new List<Message>();
            }

            _messagesByUser[message.UserId].Add(message);

            _logger.LogDebug(
                "Archived message {MessageId} for user {UserId}",
                message.Id,
                message.UserId);
        }
    }

    public async Task<Message[]> SearchAsync(
        string userId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (!_messagesByUser.ContainsKey(userId))
            {
                return Array.Empty<Message>();
            }

            var queryLower = query.ToLowerInvariant();

            // Simple keyword search (in production, use vector similarity)
            var results = _messagesByUser[userId]
                .Where(m => m.Content.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.Metadata.ImportanceScore)
                .ThenByDescending(m => m.Timestamp)
                .Take(maxResults)
                .ToArray();

            _logger.LogDebug(
                "Found {Count} archived messages for query: {Query}",
                results.Length,
                query);

            return results;
        }
    }

    public async Task<Message?> GetAsync(string messageId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            return _messagesById.TryGetValue(messageId, out var message) ? message : null;
        }
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            // Find all message IDs for this user
            var messageIdsToRemove = _messagesByUser.ContainsKey(userId)
                ? _messagesByUser[userId].Select(m => m.Id).ToList()
                : new List<string>();

            // Remove from both dictionaries
            if (_messagesByUser.ContainsKey(userId))
            {
                _messagesByUser.Remove(userId);
            }

            foreach (var messageId in messageIdsToRemove)
            {
                _messagesById.Remove(messageId);
            }

            _logger.LogInformation(
                "Deleted all episodic memory data for user {UserId}: {Count} messages removed",
                userId,
                messageIdsToRemove.Count);
        }
    }
}

/// <summary>
/// In-memory Procedural Memory Service implementation for MVP/testing.
/// Implements Layer P - Learned patterns and routines.
/// </summary>
public class InMemoryProceduralMemoryService : IProceduralMemoryService
{
    private readonly Dictionary<string, List<ProceduralPattern>> _patternsByUser = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryProceduralMemoryService> _logger;

    public InMemoryProceduralMemoryService(ILogger<InMemoryProceduralMemoryService> logger)
    {
        _logger = logger;
    }

    public async Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (!_patternsByUser.ContainsKey(userId))
            {
                return null;
            }

            var queryLower = query.ToLowerInvariant();

            // Find best matching pattern
            ProceduralPattern? bestMatch = null;
            double bestScore = 0;

            foreach (var pattern in _patternsByUser[userId])
            {
                // Check keyword triggers
                foreach (var trigger in pattern.Triggers.Where(t => t.Type == Domain.Enums.TriggerType.Keyword))
                {
                    if (queryLower.Contains(trigger.Pattern.ToLowerInvariant()))
                    {
                        var score = 0.8; // Simple match score

                        if (score > bestScore && pattern.Matches(query, score))
                        {
                            bestScore = score;
                            bestMatch = pattern;
                        }
                    }
                }
            }

            if (bestMatch != null)
            {
                bestMatch.RecordUsage();

                _logger.LogInformation(
                    "Matched procedural pattern: {PatternName}",
                    bestMatch.Name);
            }

            return bestMatch;
        }
    }

    public async Task StorePatternAsync(
        ProceduralPattern pattern,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (!_patternsByUser.ContainsKey(pattern.UserId))
            {
                _patternsByUser[pattern.UserId] = new List<ProceduralPattern>();
            }

            _patternsByUser[pattern.UserId].Add(pattern);

            _logger.LogDebug(
                "Stored procedural pattern: {PatternName} for user {UserId}",
                pattern.Name,
                pattern.UserId);
        }
    }

    public async Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        // Simple pattern detection (in production, use LLM)
        var content = message.Content.ToLowerInvariant();

        // Detect procedural instructions like "always...", "never...", "from now on..."
        if (content.Contains("always") || content.Contains("never") || content.Contains("from now on"))
        {
            var pattern = new ProceduralPattern
            {
                UserId = userId,
                Name = $"Auto-detected pattern from message {message.Id[..8]}",
                Description = message.Content.Length > 100
                    ? message.Content[..100] + "..."
                    : message.Content,
                Triggers = new[]
                {
                    new PatternTrigger
                    {
                        Type = Domain.Enums.TriggerType.Keyword,
                        Pattern = ExtractKeyword(message.Content)
                    }
                },
                InstructionTemplate = message.Content,
                ConfidenceThreshold = 0.7,
                UsageCount = 0,
                CreatedAt = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow
            };

            await StorePatternAsync(pattern, cancellationToken);

            _logger.LogInformation(
                "Auto-detected and stored procedural pattern for user {UserId}",
                userId);
        }
    }

    public async Task<ProceduralPattern[]> GetUserPatternsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        lock (_lock)
        {
            if (!_patternsByUser.ContainsKey(userId))
            {
                return Array.Empty<ProceduralPattern>();
            }

            return _patternsByUser[userId]
                .OrderByDescending(p => p.UsageCount)
                .ToArray();
        }
    }

    private string ExtractKeyword(string content)
    {
        // Simple keyword extraction (first significant word)
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for" };

        return words.FirstOrDefault(w =>
            w.Length > 3 &&
            !stopWords.Contains(w.ToLowerInvariant())) ?? "general";
    }
}
