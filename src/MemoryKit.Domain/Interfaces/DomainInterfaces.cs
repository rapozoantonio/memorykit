using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Domain.Interfaces;

/// <summary>
/// Interface for a memory layer in the hierarchical memory system.
/// </summary>
public interface IMemoryLayer
{
    /// <summary>
    /// Retrieves relevant items from this memory layer based on a query.
    /// </summary>
    Task<T[]> RetrieveAsync<T>(string query, int maxResults, CancellationToken cancellationToken = default)
        where T : notnull;

    /// <summary>
    /// Stores an item in this memory layer.
    /// </summary>
    Task StoreAsync<T>(T item, CancellationToken cancellationToken = default)
        where T : notnull;

    /// <summary>
    /// Gets the latency in milliseconds for typical retrieval operations.
    /// </summary>
    Task<double> GetAverageLatencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears or prunes expired items from this memory layer.
    /// </summary>
    Task PruneAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for orchestrating retrieval from multiple memory layers.
/// </summary>
public interface IMemoryOrchestrator
{
    /// <summary>
    /// Retrieves and assembles memory context for a given query.
    /// </summary>
    Task<MemoryContext> RetrieveContextAsync(
        string userId,
        string conversationId,
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a message across all appropriate memory layers.
    /// </summary>
    Task StoreAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds an optimized query plan based on the query content.
    /// </summary>
    Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges all data for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for calculating importance scores using the Amygdala model.
/// </summary>
public interface IImportanceEngine
{
    /// <summary>
    /// Calculates the importance score for a message.
    /// </summary>
    Task<ImportanceScore> CalculateImportanceAsync(
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the importance score for extracted entities.
    /// </summary>
    Task<double> CalculateEntityImportanceAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the current state of a conversation for planning purposes.
/// </summary>
public record ConversationState
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the conversation ID.
    /// </summary>
    public required string ConversationId { get; init; }

    /// <summary>
    /// Gets the message count in this conversation.
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// Gets the number of turns in the conversation.
    /// </summary>
    public int TurnCount { get; init; }

    /// <summary>
    /// Gets the elapsed time since conversation started.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Gets the number of queries in this session.
    /// </summary>
    public int QueryCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the last query.
    /// </summary>
    public DateTime LastQueryTime { get; init; }

    /// <summary>
    /// Gets the average query/response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the last activity timestamp for this conversation.
    /// </summary>
    public DateTime LastActivity { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the assembled context for responding to a query.
/// </summary>
public record MemoryContext
{
    /// <summary>
    /// Gets the recent messages from working memory.
    /// </summary>
    public required Message[] WorkingMemory { get; init; }

    /// <summary>
    /// Gets the extracted facts from semantic memory.
    /// </summary>
    public required ExtractedFact[] Facts { get; init; }

    /// <summary>
    /// Gets the archived messages from episodic memory.
    /// </summary>
    public required Message[] ArchivedMessages { get; init; }

    /// <summary>
    /// Gets the procedural pattern if one was matched.
    /// </summary>
    public ProceduralPattern? AppliedProcedure { get; init; }

    /// <summary>
    /// Gets the query plan that was executed.
    /// </summary>
    public required QueryPlan QueryPlan { get; init; }

    /// <summary>
    /// Gets the total estimated tokens in this context.
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Gets the retrieval latency in milliseconds.
    /// </summary>
    public long RetrievalLatencyMs { get; init; }

    /// <summary>
    /// Formats the context as a prompt for the LLM.
    /// </summary>
    public string ToPromptContext()
    {
        var sb = new System.Text.StringBuilder();

        // Apply procedural instruction if matched
        if (AppliedProcedure != null)
        {
            sb.AppendLine($"[SYSTEM INSTRUCTION]: {AppliedProcedure.InstructionTemplate}");
            sb.AppendLine();
        }

        // Recent conversation
        if (WorkingMemory.Any())
        {
            sb.AppendLine("=== Recent Conversation ===");
            foreach (var msg in WorkingMemory.OrderBy(m => m.Timestamp))
            {
                sb.AppendLine($"{msg.Role}: {msg.Content}");
            }
            sb.AppendLine();
        }

        // Relevant facts
        if (Facts.Any())
        {
            sb.AppendLine("=== Relevant Facts ===");
            foreach (var fact in Facts.OrderByDescending(f => f.Importance).Take(10))
            {
                sb.AppendLine($"- {fact.Key}: {fact.Value}");
            }
            sb.AppendLine();
        }

        // Archived context (if deep recall needed)
        if (ArchivedMessages.Any())
        {
            sb.AppendLine("=== Previous Relevant Exchanges ===");
            foreach (var msg in ArchivedMessages.OrderBy(m => m.Timestamp))
            {
                sb.AppendLine($"[{msg.Timestamp:yyyy-MM-dd HH:mm}] {msg.Role}: {msg.Content}");
            }
        }

        return sb.ToString();
    }
}

// ============================================================================
// MEMORY SERVICE INTERFACES
// ============================================================================

/// <summary>
/// Working Memory Service using Redis Cache or in-memory storage.
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

    /// <summary>
    /// Removes a specific message from working memory.
    /// </summary>
    Task RemoveAsync(
        string userId,
        string conversationId,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all working memory data for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Scratchpad Service using Azure Table Storage or in-memory storage.
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

    /// <summary>
    /// Deletes a specific fact by ID.
    /// </summary>
    Task DeleteFactAsync(string userId, string factId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all semantic memory data for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Episodic Memory Service using Azure Blob Storage and AI Search or in-memory storage.
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

    /// <summary>
    /// Deletes a specific archived message.
    /// </summary>
    Task DeleteAsync(string userId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all episodic memory data for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Procedural Memory Service using Azure Table Storage or in-memory storage.
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

    /// <summary>
    /// Deletes all procedural memory data for a user (GDPR compliance).
    /// </summary>
    Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

// ============================================================================
// COGNITIVE SERVICE INTERFACES
// ============================================================================

/// <summary>
/// Amygdala Importance Engine - calculates emotional weighting and importance scores.
/// Inspired by the amygdala's role in emotional tagging of memories.
/// </summary>
public interface IAmygdalaImportanceEngine : IImportanceEngine
{
    /// <summary>
    /// Analyzes sentiment of a message.
    /// </summary>
    Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects decision or commitment language.
    /// </summary>
    bool ContainsDecisionLanguage(string text);

    /// <summary>
    /// Detects explicit importance markers.
    /// </summary>
    bool HasExplicitImportanceMarkers(string text);
}

/// <summary>
/// Hippocampus Indexer - handles initial encoding and indexing of information.
/// Inspired by the hippocampus's role in consolidating short-term to long-term memory.
/// </summary>
public interface IHippocampusIndexer
{
    /// <summary>
    /// Encodes and indexes a message for later consolidation.
    /// </summary>
    Task<string> EncodeAsync(
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message for consolidation to long-term memory.
    /// </summary>
    Task MarkForConsolidationAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs consolidation of encoded messages to long-term storage.
    /// </summary>
    Task ConsolidateAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs consolidation for a specific conversation.
    /// </summary>
    Task ConsolidateAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single message immediately.
    /// </summary>
    Task IndexAsync(
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prunes old memories based on retention period.
    /// </summary>
    Task PruneOldMemoriesAsync(
        string userId,
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets consolidation metrics for a user.
    /// </summary>
    Task<ConsolidationMetrics> GetConsolidationMetricsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Metrics about memory consolidation.
/// </summary>
public record ConsolidationMetrics
{
    /// <summary>
    /// Gets or initializes the user ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the timestamp of the last consolidation.
    /// </summary>
    public DateTime LastConsolidation { get; init; }

    /// <summary>
    /// Gets or initializes the number of messages consolidated.
    /// </summary>
    public int MessagesConsolidated { get; init; }

    /// <summary>
    /// Gets or initializes the average importance score.
    /// </summary>
    public double AverageImportanceScore { get; init; }
}

/// <summary>
/// Prefrontal Controller - orchestrates query classification and planning.
/// Inspired by the prefrontal cortex's role in executive function and attention control.
/// </summary>
public interface IPrefrontalController
{
    /// <summary>
    /// Builds an optimized query plan based on query content and conversation state.
    /// </summary>
    Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies the type of query.
    /// </summary>
    Task<QueryType> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines which memory layers should be consulted.
    /// </summary>
    List<MemoryLayer> DetermineLayersToUse(QueryType queryType, ConversationState state);
}

// ============================================================================
// SEMANTIC KERNEL SERVICE INTERFACE
// ============================================================================

/// <summary>
/// Service for interacting with Semantic Kernel for text generation and embeddings.
/// </summary>
public interface ISemanticKernelService
{
    /// <summary>
    /// Generates embeddings for a given text.
    /// </summary>
    Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies a query into a type for query planning.
    /// </summary>
    Task<string> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts structured entities from text using an LLM.
    /// </summary>
    Task<ExtractedEntity[]> ExtractEntitiesAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a completion using the configured LLM.
    /// </summary>
    Task<string> CompleteAsync(
        string prompt,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an answer to a question using the provided memory context.
    /// </summary>
    Task<string> AnswerWithContextAsync(
        string question,
        MemoryContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes text sentiment using an LLM.
    /// </summary>
    Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(
        string text,
        CancellationToken cancellationToken = default);
}
