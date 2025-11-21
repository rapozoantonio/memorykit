using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Application.Services;

/// <summary>
/// Prefrontal Cortex analog: Query classification and retrieval strategy.
/// Determines which memory layers to query based on the query type.
/// </summary>
public class PrefrontalController : IPrefrontalController
{
    private readonly ILogger<PrefrontalController> _logger;

    public PrefrontalController(ILogger<PrefrontalController> logger)
    {
        _logger = logger;
    }

    public Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default)
    {
        // Fast rule-based classification first
        var quickType = QuickClassify(query);

        if (quickType.HasValue)
        {
            _logger.LogDebug("Quick classified query as {QueryType}", quickType.Value);
            return Task.FromResult(CreatePlan(quickType.Value, state));
        }

        // Default to complex for unclassified queries
        _logger.LogDebug("Could not quick classify, defaulting to Complex query type");
        return Task.FromResult(CreatePlan(QueryType.Complex, state));
    }

    public Task<QueryType> ClassifyQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var quickType = QuickClassify(query);
        return Task.FromResult(quickType ?? QueryType.Complex);
    }

    public List<MemoryLayer> DetermineLayersToUse(QueryType queryType, ConversationState state)
    {
        var plan = CreatePlan(queryType, state);
        return plan.LayersToUse;
    }

    /// <summary>
    /// Quick pattern-based classification for common query types.
    /// </summary>
    public virtual QueryType? QuickClassify(string query)
    {
        var lower = query.ToLowerInvariant().Trim();

        // Continuation patterns
        if (ContinuationPatterns.Any(p => lower.StartsWith(p)))
            return QueryType.Continuation;

        // Fact retrieval patterns
        if (FactRetrievalPatterns.Any(p => lower.Contains(p)))
            return QueryType.FactRetrieval;

        // Deep recall patterns
        if (DeepRecallPatterns.Any(p => lower.Contains(p)))
            return QueryType.DeepRecall;

        // Procedural trigger patterns
        if (ProceduralTriggerPatterns.Any(p => lower.Contains(p)))
            return QueryType.ProceduralTrigger;

        return null; // Needs complex classification
    }

    /// <summary>
    /// Creates a query plan based on the query type.
    /// </summary>
    public virtual QueryPlan CreatePlan(QueryType type, ConversationState state)
    {
        var plan = type switch
        {
            QueryType.Continuation => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory
                },
                EstimatedTokens = 200
            },

            QueryType.FactRetrieval => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory,
                    MemoryLayer.SemanticMemory
                },
                EstimatedTokens = 500
            },

            QueryType.DeepRecall => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory,
                    MemoryLayer.SemanticMemory,
                    MemoryLayer.EpisodicMemory
                },
                EstimatedTokens = 1500
            },

            QueryType.ProceduralTrigger => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory,
                    MemoryLayer.ProceduralMemory
                },
                EstimatedTokens = 300
            },

            QueryType.Complex => new QueryPlan
            {
                Type = type,
                LayersToUse = Enum.GetValues<MemoryLayer>().ToList(),
                EstimatedTokens = 2000
            },

            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        _logger.LogInformation(
            "Created query plan: Type={Type}, Layers={Layers}, EstimatedTokens={Tokens}",
            plan.Type,
            string.Join(", ", plan.LayersToUse),
            plan.EstimatedTokens);

        return plan;
    }

    // Pattern definitions
    private static readonly string[] ContinuationPatterns = new[]
    {
        "continue", "go on", "and then", "next", "keep going", "more"
    };

    private static readonly string[] FactRetrievalPatterns = new[]
    {
        "what was", "what is", "who is", "when did", "where", "how many",
        "tell me about", "remind me"
    };

    private static readonly string[] DeepRecallPatterns = new[]
    {
        "quote", "exactly", "verbatim", "word for word", "precise",
        "show me the", "find the conversation"
    };

    private static readonly string[] ProceduralTriggerPatterns = new[]
    {
        "write code", "create", "generate", "build", "implement",
        "format", "structure"
    };
}
