using MemoryKit.Domain.Entities;
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
    /// Gets the conversation ID.
    /// </summary>
    public required string ConversationId { get; init; }

    /// <summary>
    /// Gets the message count in this conversation.
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// Gets the elapsed time since conversation started.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Gets the number of queries in this session.
    /// </summary>
    public int QueryCount { get; init; }

    /// <summary>
    /// Gets the average query/response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }
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
