using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Infrastructure.Azure;

/// <summary>
/// Working Memory Service using Redis Cache.
/// Provides sub-5ms retrieval for recent context.
/// </summary>
public interface IWorkingMemoryService
{
    /// <summary>
    /// Adds a message to working memory.
    /// </summary>
    Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent messages from working memory.
    /// </summary>
    Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears working memory for a conversation.
    /// </summary>
    Task ClearAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Scratchpad Service using Azure Table Storage.
/// Provides semantic memory with vector indexing.
/// </summary>
public interface IScratchpadService
{
    /// <summary>
    /// Stores extracted facts.
    /// </summary>
    Task StoreFactsAsync(
        string userId,
        string conversationId,
        ExtractedFact[] facts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for relevant facts.
    /// </summary>
    Task<ExtractedFact[]> SearchFactsAsync(
        string userId,
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates fact access tracking.
    /// </summary>
    Task RecordAccessAsync(string factId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prunes expired or unused facts.
    /// </summary>
    Task PruneAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Episodic Memory Service using Azure Blob Storage and AI Search.
/// Provides full conversation archive with vector search.
/// </summary>
public interface IEpisodicMemoryService
{
    /// <summary>
    /// Archives a message to blob and indexes it.
    /// </summary>
    Task ArchiveAsync(
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches archived messages.
    /// </summary>
    Task<Message[]> SearchAsync(
        string userId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific archived message.
    /// </summary>
    Task<Message?> GetAsync(string messageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Procedural Memory Service using Azure Table Storage.
/// Stores learned patterns and routines.
/// </summary>
public interface IProceduralMemoryService
{
    /// <summary>
    /// Matches a query against learned patterns.
    /// </summary>
    Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a procedural pattern.
    /// </summary>
    Task StorePatternAsync(
        ProceduralPattern pattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects and stores new patterns from messages.
    /// </summary>
    Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user's patterns.
    /// </summary>
    Task<ProceduralPattern[]> GetUserPatternsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
