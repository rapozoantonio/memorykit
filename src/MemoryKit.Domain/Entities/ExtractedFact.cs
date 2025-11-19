using MemoryKit.Domain.Common;
using MemoryKit.Domain.Enums;

namespace MemoryKit.Domain.Entities;

/// <summary>
/// Represents an extracted fact from conversation content.
/// </summary>
public class ExtractedFact : Entity<string>
{
    /// <summary>
    /// Gets the user ID who owns this fact.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the conversation ID this fact belongs to.
    /// </summary>
    public string ConversationId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the key or name of the fact.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the value or content of the fact.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the type of entity this fact represents.
    /// </summary>
    public EntityType Type { get; private set; }

    /// <summary>
    /// Gets the importance score (0.0 to 1.0).
    /// </summary>
    public double Importance { get; private set; }

    /// <summary>
    /// Gets the last time this fact was accessed.
    /// </summary>
    public DateTime LastAccessed { get; private set; }

    /// <summary>
    /// Gets the number of times this fact has been accessed.
    /// </summary>
    public int AccessCount { get; private set; }

    /// <summary>
    /// Gets the embedding vector for semantic search.
    /// </summary>
    public float[]? Embedding { get; private set; }

    /// <summary>
    /// Factory method to create a new fact.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when userId, conversationId, key, or value is null or whitespace.</exception>
    public static ExtractedFact Create(
        string userId,
        string conversationId,
        string key,
        string value,
        EntityType type,
        double importance = 0.5)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation ID cannot be null or whitespace", nameof(conversationId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace", nameof(key));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace", nameof(value));

        return new ExtractedFact
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ConversationId = conversationId,
            Key = key,
            Value = value,
            Type = type,
            Importance = Math.Clamp(importance, 0.0, 1.0),
            LastAccessed = DateTime.UtcNow,
            AccessCount = 1
        };
    }

    /// <summary>
    /// Records an access to this fact for tracking purposes.
    /// </summary>
    public void RecordAccess()
    {
        LastAccessed = DateTime.UtcNow;
        AccessCount++;
    }

    /// <summary>
    /// Determines whether this fact should be evicted based on usage and age.
    /// </summary>
    public bool ShouldEvict(TimeSpan ttl, int minAccessCount)
    {
        var age = DateTime.UtcNow - LastAccessed;
        return AccessCount < minAccessCount && age > ttl;
    }

    /// <summary>
    /// Sets the embedding vector for semantic search.
    /// </summary>
    public void SetEmbedding(float[] embedding)
    {
        Embedding = embedding;
    }

    /// <summary>
    /// Updates the importance score.
    /// </summary>
    public void UpdateImportance(double newScore)
    {
        Importance = Math.Clamp(newScore, 0.0, 1.0);
        UpdatedAt = DateTime.UtcNow;
    }
}
