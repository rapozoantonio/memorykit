using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Infrastructure.Cognitive;

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
