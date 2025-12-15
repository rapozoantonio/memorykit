using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.InMemory;

/// <summary>
/// In-memory Working Memory Service implementation.
/// Implements Layer 3 (L3) - Hot context for active conversations with TTL-based cleanup.
/// </summary>
public class InMemoryWorkingMemoryService : IWorkingMemoryService
{
    private readonly Dictionary<string, ConversationCache> _storage = new();
    private readonly object _lock = new();
    private const int MaxItems = 10;
    private const int MaxConversations = 10000; // Prevent unbounded growth
    private static readonly TimeSpan ConversationTtl = TimeSpan.FromHours(24); // 24 hour TTL
    private readonly ILogger<InMemoryWorkingMemoryService> _logger;
    private DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    public InMemoryWorkingMemoryService(ILogger<InMemoryWorkingMemoryService> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = GetKey(userId, conversationId);

            if (!_storage.ContainsKey(key))
            {
                _storage[key] = new ConversationCache
                {
                    Messages = new List<Message>(),
                    LastAccessed = DateTime.UtcNow
                };
            }

            _storage[key].Messages.Add(message);
            _storage[key].LastAccessed = DateTime.UtcNow;

            // Keep only most recent/important items
            if (_storage[key].Messages.Count > MaxItems)
            {
                // Remove least important oldest message
                var toRemove = _storage[key].Messages
                    .MinBy(m => (m.Metadata.ImportanceScore, m.Timestamp));

                if (toRemove != null)
                {
                    _storage[key].Messages.Remove(toRemove);
                }
            }

            _logger.LogDebug(
                "Added message to working memory: {ConversationId}, Total: {Count}",
                conversationId,
                _storage[key].Messages.Count);

            // Periodic cleanup
            PerformPeriodicCleanup();
        }

        return Task.CompletedTask;
    }

    public Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = GetKey(userId, conversationId);

            if (!_storage.ContainsKey(key))
            {
                return Task.FromResult(Array.Empty<Message>());
            }

            _storage[key].LastAccessed = DateTime.UtcNow;

            return Task.FromResult(_storage[key].Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToArray());
        }
    }

    public Task ClearAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKey(userId, conversationId);
            _storage.Remove(key);

            _logger.LogDebug("Cleared working memory for conversation: {ConversationId}", conversationId);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        string userId,
        string conversationId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKey(userId, conversationId);

            if (_storage.ContainsKey(key))
            {
                var message = _storage[key].Messages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    _storage[key].Messages.Remove(message);
                    _logger.LogDebug("Removed message {MessageId} from working memory", messageId);
                }
            }
        }

        return Task.CompletedTask;
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

    /// <summary>
    /// Performs periodic cleanup of expired conversations (must be called within lock).
    /// </summary>
    private void PerformPeriodicCleanup()
    {
        var now = DateTime.UtcNow;

        if (now - _lastCleanup < CleanupInterval && _storage.Count < MaxConversations)
        {
            return; // Not yet time for cleanup
        }

        _lastCleanup = now;
        var cutoffTime = now - ConversationTtl;
        var beforeCount = _storage.Count;

        // Remove expired conversations
        var expiredKeys = _storage
            .Where(kvp => kvp.Value.LastAccessed < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _storage.Remove(key);
        }

        // If still over limit, remove oldest conversations (LRU eviction)
        if (_storage.Count > MaxConversations)
        {
            var toEvict = _storage
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_storage.Count - MaxConversations)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toEvict)
            {
                _storage.Remove(key);
            }

            _logger.LogWarning(
                "LRU eviction: Removed {Count} conversations to stay under limit of {MaxConversations}",
                toEvict.Count,
                MaxConversations);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation(
                "Cleaned up {ExpiredCount} expired conversations (before: {Before}, after: {After})",
                expiredKeys.Count,
                beforeCount,
                _storage.Count);
        }
    }

    private static string GetKey(string userId, string conversationId)
        => $"{userId}:{conversationId}";

    /// <summary>
    /// Cached conversation data with last access tracking.
    /// </summary>
    private class ConversationCache
    {
        public List<Message> Messages { get; set; } = new();
        public DateTime LastAccessed { get; set; }
    }
}

/// <summary>
/// In-memory Scratchpad Service implementation.
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

    public Task StoreFactsAsync(
        string userId,
        string conversationId,
        ExtractedFact[] facts,
        CancellationToken cancellationToken = default)
    {
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

        return Task.CompletedTask;
    }

    public Task<ExtractedFact[]> SearchFactsAsync(
        string userId,
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_storage.ContainsKey(userId))
            {
                return Task.FromResult(Array.Empty<ExtractedFact>());
            }

            var queryLower = query.ToLowerInvariant();

            // Simple keyword matching
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

            return Task.FromResult(results);
        }
    }

    public Task RecordAccessAsync(string factId, CancellationToken cancellationToken = default)
    {

        lock (_lock)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

        return Task.CompletedTask;
    }

    public Task PruneAsync(string userId, CancellationToken cancellationToken = default)
    {

        lock (_lock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_storage.ContainsKey(userId))
            {
                return Task.CompletedTask;
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

        return Task.CompletedTask;
    }

    public Task DeleteFactAsync(string userId, string factId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_storage.ContainsKey(userId))
            {
                var fact = _storage[userId].FirstOrDefault(f => f.Id == factId);
                if (fact != null)
                {
                    _storage[userId].Remove(fact);
                    _logger.LogDebug("Deleted fact {FactId} for user {UserId}", factId, userId);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {

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

        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory Episodic Memory Service implementation.
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

    public Task ArchiveAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {

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

        return Task.CompletedTask;
    }

    public Task<Message[]> SearchAsync(
        string userId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_messagesByUser.ContainsKey(userId))
            {
                return Task.FromResult(Array.Empty<Message>());
            }

            var queryLower = query.ToLowerInvariant();

            // Simple keyword search
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

            return Task.FromResult(results);
        }
    }

    public Task<Message?> GetAsync(string messageId, CancellationToken cancellationToken = default)
    {

        lock (_lock)
        {
            return Task.FromResult(_messagesById.TryGetValue(messageId, out var message) ? message : null);
        }
    }

    public Task DeleteAsync(string userId, string messageId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_messagesById.TryGetValue(messageId, out var message))
            {
                // Verify the message belongs to the user
                if (message.UserId == userId)
                {
                    _messagesById.Remove(messageId);

                    if (_messagesByUser.ContainsKey(userId))
                    {
                        _messagesByUser[userId].Remove(message);
                    }

                    _logger.LogDebug("Deleted message {MessageId} from episodic memory", messageId);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
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

        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory Procedural Memory Service implementation.
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

    public Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {

        lock (_lock)
        {
            if (!_patternsByUser.ContainsKey(userId))
            {
                return Task.FromResult<ProceduralPattern?>(null);
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

            return Task.FromResult(bestMatch);
        }
    }

    public Task StorePatternAsync(
        ProceduralPattern pattern,
        CancellationToken cancellationToken = default)
    {
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

        return Task.CompletedTask;
    }

    public async Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        // Simple pattern detection
        var content = message.Content.ToLowerInvariant();

        // Detect procedural instructions like "always...", "never...", "from now on..."
        if (content.Contains("always") || content.Contains("never") || content.Contains("from now on"))
        {
            var pattern = ProceduralPattern.Create(
                userId: userId,
                name: $"Auto-detected pattern from message {message.Id[..8]}",
                description: message.Content.Length > 100
                    ? message.Content[..100] + "..."
                    : message.Content,
                triggers: new[]
                {
                    new PatternTrigger
                    {
                        Type = Domain.Enums.TriggerType.Keyword,
                        Pattern = ExtractKeyword(message.Content)
                    }
                },
                instructionTemplate: message.Content,
                confidenceThreshold: 0.7
            );

            await StorePatternAsync(pattern, cancellationToken);

            _logger.LogInformation(
                "Auto-detected and stored procedural pattern for user {UserId}",
                userId);
        }
    }

    public Task<ProceduralPattern[]> GetUserPatternsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {

        lock (_lock)
        {
            if (!_patternsByUser.ContainsKey(userId))
            {
                return Task.FromResult(Array.Empty<ProceduralPattern>());
            }

            return Task.FromResult(_patternsByUser[userId]
                .OrderByDescending(p => p.UsageCount)
                .ToArray());
        }
    }

    public Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_patternsByUser.ContainsKey(userId))
            {
                _patternsByUser.Remove(userId);
                _logger.LogInformation("Deleted all procedural patterns for user {UserId}", userId);
            }
        }

        return Task.CompletedTask;
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
