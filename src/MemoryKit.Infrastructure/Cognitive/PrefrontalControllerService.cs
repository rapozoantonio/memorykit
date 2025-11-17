using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Cognitive;

/// <summary>
/// Prefrontal Controller implementation - query classification and retrieval strategy.
/// Implements TRD Section 6.2.
/// </summary>
public class PrefrontalControllerService : IPrefrontalController
{
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<PrefrontalControllerService> _logger;

    // Fast rule-based classification patterns
    private static readonly string[] ContinuationPatterns = new[]
    {
        "continue", "go on", "and then", "next", "keep going", "tell me more"
    };

    private static readonly string[] FactRetrievalPatterns = new[]
    {
        "what was", "what is", "who is", "when did", "where", "how many",
        "tell me about", "remind me"
    };

    private static readonly string[] DeepRecallPatterns = new[]
    {
        "quote", "exactly", "verbatim", "word for word", "precise",
        "show me the exact", "what did i say about"
    };

    private static readonly string[] ProceduralPatterns = new[]
    {
        "write code", "create", "build", "implement", "show me how",
        "generate", "make"
    };

    public PrefrontalControllerService(
        ISemanticKernelService llm,
        ILogger<PrefrontalControllerService> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    public async Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Building query plan for: {Query}", query);

        // Fast rule-based classification first
        var quickType = QuickClassify(query);

        QueryType classifiedType;
        if (quickType.HasValue)
        {
            classifiedType = quickType.Value;
            _logger.LogDebug(
                "Quick classification: {Type}",
                classifiedType);
        }
        else
        {
            // Use LLM for complex queries
            classifiedType = await ClassifyQueryAsync(query, cancellationToken);
            _logger.LogDebug(
                "LLM classification: {Type}",
                classifiedType);
        }

        var plan = CreatePlan(classifiedType, state);

        _logger.LogInformation(
            "Query plan created: Type={Type}, Layers={LayerCount}",
            plan.Type,
            plan.LayersToUse.Count);

        return plan;
    }

    public async Task<QueryType> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Quick check first
        var quickType = QuickClassify(query);
        if (quickType.HasValue)
        {
            return quickType.Value;
        }

        try
        {
            // Use LLM for complex classification
            var classification = await _llm.ClassifyQueryAsync(query, cancellationToken);

            if (Enum.TryParse<QueryType>(classification, ignoreCase: true, out var result))
            {
                return result;
            }

            _logger.LogWarning(
                "LLM returned unexpected classification: {Classification}. Defaulting to Complex.",
                classification);

            return QueryType.Complex;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error classifying query with LLM, defaulting to Complex");

            return QueryType.Complex;
        }
    }

    public List<MemoryLayer> DetermineLayersToUse(QueryType queryType, ConversationState state)
    {
        return queryType switch
        {
            QueryType.Continuation => new List<MemoryLayer>
            {
                MemoryLayer.WorkingMemory
            },

            QueryType.FactRetrieval => new List<MemoryLayer>
            {
                MemoryLayer.WorkingMemory,
                MemoryLayer.SemanticMemory
            },

            QueryType.DeepRecall => new List<MemoryLayer>
            {
                MemoryLayer.WorkingMemory,
                MemoryLayer.SemanticMemory,
                MemoryLayer.EpisodicMemory
            },

            QueryType.Complex => Enum.GetValues<MemoryLayer>().ToList(),

            QueryType.ProceduralTrigger => new List<MemoryLayer>
            {
                MemoryLayer.WorkingMemory,
                MemoryLayer.ProceduralMemory
            },

            _ => new List<MemoryLayer> { MemoryLayer.WorkingMemory }
        };
    }

    /// <summary>
    /// Fast rule-based query classification.
    /// Returns null if uncertain and LLM classification is needed.
    /// </summary>
    private QueryType? QuickClassify(string query)
    {
        var lower = query.ToLowerInvariant().Trim();

        // Continuation patterns
        if (ContinuationPatterns.Any(p => lower.StartsWith(p)))
        {
            return QueryType.Continuation;
        }

        // Procedural patterns (check before fact retrieval)
        if (ProceduralPatterns.Any(p => lower.Contains(p)))
        {
            return QueryType.ProceduralTrigger;
        }

        // Deep recall patterns
        if (DeepRecallPatterns.Any(p => lower.Contains(p)))
        {
            return QueryType.DeepRecall;
        }

        // Fact retrieval patterns
        if (FactRetrievalPatterns.Any(p => lower.StartsWith(p) || lower.Contains(p)))
        {
            return QueryType.FactRetrieval;
        }

        // Unable to classify with rules - needs LLM
        return null;
    }

    /// <summary>
    /// Creates a query plan based on classified type and conversation state.
    /// </summary>
    private QueryPlan CreatePlan(QueryType type, ConversationState state)
    {
        var layers = DetermineLayersToUse(type, state);

        // Estimate tokens based on layers
        var estimatedTokens = layers.Sum(layer => layer switch
        {
            MemoryLayer.WorkingMemory => 500,      // ~10 messages * 50 tokens
            MemoryLayer.SemanticMemory => 400,     // ~20 facts * 20 tokens
            MemoryLayer.EpisodicMemory => 300,     // ~5 messages * 60 tokens
            MemoryLayer.ProceduralMemory => 100,   // ~1 pattern instruction
            _ => 0
        });

        return new QueryPlan
        {
            Type = type,
            LayersToUse = layers,
            EstimatedTokens = estimatedTokens
        };
    }
}
