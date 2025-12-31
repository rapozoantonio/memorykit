using MemoryKit.Domain.Entities;

namespace MemoryKit.Infrastructure.PostgreSQL.Repositories;

/// <summary>
/// Repository interface for Working Memory persistence.
/// Working Memory stores short-term, active context (<5ms access).
/// </summary>
public interface IWorkingMemoryRepository
{
    /// <summary>
    /// Adds a message to working memory.
    /// </summary>
    Task AddAsync(string userId, string conversationId, Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent messages from working memory.
    /// </summary>
    Task<Message[]> GetRecentAsync(string userId, string conversationId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific message from working memory.
    /// </summary>
    Task RemoveAsync(string userId, string conversationId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all messages for a conversation.
    /// </summary>
    Task ClearAsync(string userId, string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all working memory for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes expired or low-importance messages (moves them to semantic memory).
    /// </summary>
    Task<int> PromoteToSemanticAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Semantic Memory persistence.
/// Semantic Memory stores facts and knowledge (<50ms access, with vector search).
/// </summary>
public interface ISemanticMemoryRepository
{
    /// <summary>
    /// Stores a fact in semantic memory.
    /// </summary>
    Task<string> AddAsync(ExtractedFact fact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing fact.
    /// </summary>
    Task UpdateAsync(ExtractedFact fact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves facts by similarity (requires vector embedding).
    /// </summary>
    Task<ExtractedFact[]> SearchByEmbeddingAsync(
        string userId,
        float[] embedding,
        double similarityThreshold = 0.7,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves facts by exact key match.
    /// </summary>
    Task<ExtractedFact[]> GetByKeyAsync(string userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific fact by ID.
    /// </summary>
    Task<ExtractedFact?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a fact by ID.
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all facts for a user.
    /// </summary>
    Task<ExtractedFact[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all semantic memory for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes high-confidence facts to episodic memory.
    /// </summary>
    Task<int> PromoteToEpisodicAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Episodic Memory persistence.
/// Episodic Memory stores events and temporal information (<100ms access).
/// </summary>
public interface IEpisodicMemoryRepository
{
    /// <summary>
    /// Records an event in episodic memory.
    /// </summary>
    Task<string> AddEventAsync(
        string userId,
        string conversationId,
        string eventType,
        string content,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for a conversation within a time range.
    /// </summary>
    Task<EpisodicEvent[]> GetEventsByTimeRangeAsync(
        string userId,
        string conversationId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events by type.
    /// </summary>
    Task<EpisodicEvent[]> GetEventsByTypeAsync(
        string userId,
        string eventType,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific event by ID.
    /// </summary>
    Task<EpisodicEvent?> GetEventByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event.
    /// </summary>
    Task DeleteEventAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events for a user (with decay applied).
    /// </summary>
    Task<EpisodicEvent[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all episodic memory for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes detected patterns to procedural memory.
    /// </summary>
    Task<int> PromoteToProceduralAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Procedural Memory persistence.
/// Procedural Memory stores learned patterns and behaviors (<200ms access).
/// </summary>
public interface IProceduralMemoryRepository
{
    /// <summary>
    /// Records a learned pattern in procedural memory.
    /// </summary>
    Task<string> AddPatternAsync(
        string userId,
        string patternName,
        string triggerConditions,
        string learnedResponse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a pattern's success metrics.
    /// </summary>
    Task UpdatePatternAsync(ProceduralPattern pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific pattern by ID.
    /// </summary>
    Task<ProceduralPattern?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves patterns by name.
    /// </summary>
    Task<ProceduralPattern[]> GetByNameAsync(string userId, string patternName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all patterns for a user.
    /// </summary>
    Task<ProceduralPattern[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds patterns matching trigger conditions.
    /// </summary>
    Task<ProceduralPattern[]> FindByTriggersAsync(
        string userId,
        string triggerConditions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful pattern execution.
    /// </summary>
    Task RecordSuccessAsync(string patternId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed pattern execution.
    /// </summary>
    Task RecordFailureAsync(string patternId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a pattern.
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all procedural memory for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an episodic event in memory.
/// </summary>
public class EpisodicEvent
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Participants { get; set; }
    public DateTime OccurredAt { get; set; }
    public double DecayFactor { get; set; } = 1.0;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
