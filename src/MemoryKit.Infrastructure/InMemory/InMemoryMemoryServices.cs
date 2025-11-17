using System.Collections.Concurrent;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.Azure;

namespace MemoryKit.Infrastructure.InMemory;

/// <summary>
/// In-memory implementation of Working Memory Service for testing and MVP.
/// Simulates Redis with sub-5ms retrieval.
/// </summary>
public class InMemoryWorkingMemoryService : IWorkingMemoryService
{
    private readonly ConcurrentDictionary<string, Queue<Message>> _storage = new();
    private const int MaxItems = 10;

    public Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(userId, conversationId);

        var queue = _storage.GetOrAdd(key, _ => new Queue<Message>());

        lock (queue)
        {
            queue.Enqueue(message);
            while (queue.Count > MaxItems)
                queue.Dequeue();
        }

        return Task.CompletedTask;
    }

    public Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(userId, conversationId);

        if (!_storage.TryGetValue(key, out var queue))
            return Task.FromResult(Array.Empty<Message>());

        lock (queue)
        {
            return Task.FromResult(queue.TakeLast(count).ToArray());
        }
    }

    public Task ClearAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(userId, conversationId);
        _storage.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private static string GetKey(string userId, string conversationId)
        => $"{userId}:{conversationId}";
}

/// <summary>
/// In-memory implementation of Scratchpad Service for testing and MVP.
/// Simulates Azure Table Storage with semantic search.
/// </summary>
public class InMemoryScratchpadService : IScratchpadService
{
    private readonly ConcurrentDictionary<string, List<ExtractedFact>> _facts = new();
    private readonly object _lock = new();

    public Task StoreFactsAsync(
        string userId,
        string conversationId,
        ExtractedFact[] facts,
        CancellationToken cancellationToken = default)
    {
        var key = userId;
        var factList = _facts.GetOrAdd(key, _ => new List<ExtractedFact>());

        lock (_lock)
        {
            factList.AddRange(facts);
        }

        return Task.CompletedTask;
    }

    public Task<ExtractedFact[]> SearchFactsAsync(
        string userId,
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_facts.TryGetValue(userId, out var factList))
            return Task.FromResult(Array.Empty<ExtractedFact>());

        lock (_lock)
        {
            // Simple keyword matching for MVP
            var results = factList
                .Where(f =>
                    f.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    f.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Importance)
                .Take(maxResults)
                .ToArray();

            // Record access
            foreach (var fact in results)
            {
                fact.RecordAccess();
            }

            return Task.FromResult(results);
        }
    }

    public Task RecordAccessAsync(string factId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            foreach (var factList in _facts.Values)
            {
                var fact = factList.FirstOrDefault(f => f.Id == factId);
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
        if (!_facts.TryGetValue(userId, out var factList))
            return Task.CompletedTask;

        lock (_lock)
        {
            // Remove facts that haven't been accessed in 30 days and have low access count
            var ttl = TimeSpan.FromDays(30);
            factList.RemoveAll(f => f.ShouldEvict(ttl, minAccessCount: 3));
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory implementation of Episodic Memory Service for testing and MVP.
/// Simulates Azure Blob Storage and AI Search.
/// </summary>
public class InMemoryEpisodicMemoryService : IEpisodicMemoryService
{
    private readonly ConcurrentDictionary<string, Message> _archive = new();
    private readonly ConcurrentDictionary<string, List<string>> _userIndex = new();
    private readonly object _lock = new();

    public Task ArchiveAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        _archive[message.Id] = message;

        var userMessages = _userIndex.GetOrAdd(message.UserId, _ => new List<string>());
        lock (_lock)
        {
            userMessages.Add(message.Id);
        }

        return Task.CompletedTask;
    }

    public Task<Message[]> SearchAsync(
        string userId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (!_userIndex.TryGetValue(userId, out var messageIds))
            return Task.FromResult(Array.Empty<Message>());

        lock (_lock)
        {
            var results = messageIds
                .Select(id => _archive.TryGetValue(id, out var msg) ? msg : null)
                .Where(msg => msg != null)
                .Where(msg => msg!.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(msg => msg!.Timestamp)
                .Take(maxResults)
                .Cast<Message>()
                .ToArray();

            return Task.FromResult(results);
        }
    }

    public Task<Message?> GetAsync(string messageId, CancellationToken cancellationToken = default)
    {
        _archive.TryGetValue(messageId, out var message);
        return Task.FromResult(message);
    }
}

/// <summary>
/// In-memory implementation of Procedural Memory Service for testing and MVP.
/// Stores learned patterns and routines.
/// </summary>
public class InMemoryProceduralMemoryService : IProceduralMemoryService
{
    private readonly ConcurrentDictionary<string, List<ProceduralPattern>> _patterns = new();
    private readonly object _lock = new();

    public Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (!_patterns.TryGetValue(userId, out var userPatterns))
            return Task.FromResult<ProceduralPattern?>(null);

        lock (_lock)
        {
            var queryLower = query.ToLowerInvariant();

            foreach (var pattern in userPatterns.OrderByDescending(p => p.UsageCount))
            {
                foreach (var trigger in pattern.Triggers)
                {
                    if (trigger.Type == TriggerType.Keyword)
                    {
                        if (queryLower.Contains(trigger.Pattern.ToLowerInvariant()))
                        {
                            pattern.RecordUsage();
                            return Task.FromResult<ProceduralPattern?>(pattern);
                        }
                    }
                    else if (trigger.Type == TriggerType.Regex)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, trigger.Pattern))
                        {
                            pattern.RecordUsage();
                            return Task.FromResult<ProceduralPattern?>(pattern);
                        }
                    }
                }
            }
        }

        return Task.FromResult<ProceduralPattern?>(null);
    }

    public Task StorePatternAsync(
        ProceduralPattern pattern,
        CancellationToken cancellationToken = default)
    {
        var userPatterns = _patterns.GetOrAdd(pattern.UserId, _ => new List<ProceduralPattern>());

        lock (_lock)
        {
            // Check if pattern already exists
            var existing = userPatterns.FirstOrDefault(p => p.Name == pattern.Name);
            if (existing != null)
            {
                userPatterns.Remove(existing);
            }

            userPatterns.Add(pattern);
        }

        return Task.CompletedTask;
    }

    public Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        // Simplified pattern detection for MVP
        // In production, this would use LLM to detect procedural instructions

        var content = message.Content.ToLowerInvariant();

        // Detect explicit procedural instructions
        if (content.Contains("always") || content.Contains("from now on") ||
            content.Contains("remember to") || content.Contains("make sure to"))
        {
            var pattern = ProceduralPattern.Create(
                userId,
                "User Preference",
                message.Content,
                new[]
                {
                    new PatternTrigger
                    {
                        Type = TriggerType.Keyword,
                        Pattern = ExtractKeyword(content),
                        Embedding = Array.Empty<float>()
                    }
                },
                message.Content,
                confidenceThreshold: 0.7);

            return StorePatternAsync(pattern, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task<ProceduralPattern[]> GetUserPatternsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!_patterns.TryGetValue(userId, out var userPatterns))
            return Task.FromResult(Array.Empty<ProceduralPattern>());

        lock (_lock)
        {
            return Task.FromResult(userPatterns.ToArray());
        }
    }

    private static string ExtractKeyword(string content)
    {
        // Very simplified keyword extraction
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.FirstOrDefault(w => w.Length > 4) ?? "general";
    }
}
