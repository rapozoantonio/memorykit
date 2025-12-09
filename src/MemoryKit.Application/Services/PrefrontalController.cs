using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using System.Text.RegularExpressions;

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
        // Try fast pattern-based classification first for common cases
        var quickType = QuickClassify(query);
        if (quickType.HasValue)
        {
            _logger.LogDebug("Quick classified query as {QueryType}", quickType.Value);
            return Task.FromResult(CreatePlan(quickType.Value, state));
        }

        // Use advanced signal-based classification
        var context = BuildQueryContext(query, state);
        var signals = CalculateAllSignals(query, context);
        var classification = ClassifyBySignals(signals);

        _logger.LogInformation(
            "Signal-based classification: Type={Type}, Confidence={Confidence:F3}, " +
            "Signals=[Retrieval={Retrieval:F2}, Decision={Decision:F2}, Pattern={Pattern:F2}, Narrative={Narrative:F2}]",
            classification.Type,
            classification.Confidence,
            signals.RetrievalSignal,
            signals.DecisionSignal,
            signals.PatternSignal,
            signals.NarrativeSignal);

        var plan = BuildPlanFromClassification(classification, state);
        return Task.FromResult(plan);
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

    #region Signal-Based Classification

    /// <summary>
    /// Builds query context from conversation state.
    /// </summary>
    private QueryContext BuildQueryContext(string query, ConversationState state)
    {
        return new QueryContext
        {
            CurrentQuery = query,
            RecentHistory = Array.Empty<Domain.Entities.Message>(), // Could be populated from state metadata
            CurrentTopics = ExtractKeyNouns(query),
            TurnNumber = state.TurnCount
        };
    }

    /// <summary>
    /// Calculates all signal levels for the query.
    /// </summary>
    private QuerySignals CalculateAllSignals(string query, QueryContext context)
    {
        // Level 1: Surface signals (pattern-based)
        var retrievalL1 = CalculateRetrievalSignal(query);
        var decisionL1 = CalculateDecisionSignal(query);
        var patternL1 = CalculatePatternSignal(query);
        var narrativeL1 = CalculateNarrativeSignal(query);

        // Level 2: Semantic adjustments
        var hasNegation = HasNegation(query);
        retrievalL1 += CalculateNegationAdjustment(QueryType.FactRetrieval, hasNegation);
        decisionL1 += CalculateNegationAdjustment(QueryType.DeepRecall, hasNegation); // Using DeepRecall as decision proxy

        var intensity = CalculateLanguageIntensity(query);
        var highestSignal = Math.Max(Math.Max(retrievalL1, decisionL1), Math.Max(patternL1, narrativeL1));
        
        // Apply intensity boost to highest signal
        if (Math.Abs(retrievalL1 - highestSignal) < 0.01) retrievalL1 += intensity * 0.1;
        if (Math.Abs(decisionL1 - highestSignal) < 0.01) decisionL1 += intensity * 0.1;
        if (Math.Abs(patternL1 - highestSignal) < 0.01) patternL1 += intensity * 0.1;
        if (Math.Abs(narrativeL1 - highestSignal) < 0.01) narrativeL1 += intensity * 0.1;

        // Level 3: Contextual boost (simplified - could be enhanced with actual history)
        var contextBoost = CalculateContextualBoost(context);
        retrievalL1 += contextBoost * 0.1;

        // Normalize to 0-1 range
        return new QuerySignals
        {
            RetrievalSignal = Math.Min(retrievalL1, 1.0),
            DecisionSignal = Math.Min(decisionL1, 1.0),
            PatternSignal = Math.Min(patternL1, 1.0),
            NarrativeSignal = Math.Min(narrativeL1, 1.0)
        };
    }

    /// <summary>
    /// Level 1: Calculate retrieval signal (factual question detection).
    /// </summary>
    private double CalculateRetrievalSignal(string query)
    {
        double score = 0;
        var lower = query.ToLowerInvariant();
        var words = lower.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Surface signal 1: Question mark (most reliable)
        if (query.TrimEnd().EndsWith("?"))
            score += 0.25;

        // Surface signal 2: Question words at start
        var questionWords = new[] { "what", "where", "when", "who", "which", "why" };
        if (words.Length > 0 && questionWords.Any(w => words[0] == w))
            score += 0.15;

        // Surface signal 3: Question words anywhere
        if (questionWords.Any(w => lower.Contains(w)))
            score += 0.30;

        // Surface signal 4: Retrieval verbs
        var retrievalVerbs = new[] { "find", "show", "get", "tell me", "retrieve", "look up", "search", "remind me" };
        if (retrievalVerbs.Any(v => lower.Contains(v)))
            score += 0.25;

        // Surface signal 5: Length heuristic (short queries often factual)
        if (query.Length < 60)
            score += 0.10;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 1: Calculate decision signal (decision-making language detection).
    /// </summary>
    private double CalculateDecisionSignal(string query)
    {
        double score = 0;
        var lower = query.ToLowerInvariant();

        // Decision modals
        var decisionModals = new[] { "should", "shall", "ought", "must", "can we", "could we" };
        if (decisionModals.Any(m => lower.Contains(m)))
            score += 0.40;

        // Decision verbs
        var decisionVerbs = new[] { "decide", "choose", "commit", "go with", "select", "pick", "adopt", "implement" };
        if (decisionVerbs.Any(v => lower.Contains(v)))
            score += 0.35;

        // Future commitment
        if (lower.Contains(" will ") || lower.Contains("'ll ") || lower.Contains("going to"))
            score += 0.25;

        // Comparison language
        if (lower.Contains(" vs ") || lower.Contains(" or ") || lower.Contains(" versus "))
            score += 0.20;

        // Elaboration signals (longer = more likely a decision being discussed)
        if (query.Length > 80)
            score += 0.10;

        // If has question mark AND decision language, boost (deliberative questioning)
        if (query.TrimEnd().EndsWith("?") && (lower.Contains("should") || lower.Contains("decide")))
            score += 0.15;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 1: Calculate pattern signal (how-to and procedural pattern detection).
    /// </summary>
    private double CalculatePatternSignal(string query)
    {
        double score = 0;
        var lower = query.ToLowerInvariant();

        // How-to language
        var howPatterns = new[] { "how to", "how do we", "how have we", "how can we", "how should we" };
        if (howPatterns.Any(p => lower.Contains(p)))
            score += 0.45;

        // Pattern universals
        var universals = new[] { "always", "never", "every time", "whenever", "in all cases", "every case" };
        if (universals.Any(u => lower.Contains(u)))
            score += 0.35;

        // Pattern vocabulary
        var patternWords = new[] { "pattern", "approach", "method", "strategy", "process", "workflow", "procedure" };
        if (patternWords.Any(w => lower.Contains(w)))
            score += 0.30;

        // Past tense + procedural verbs indicate learned patterns
        var pastTenseIndicators = new[] { "have done", "we used to", "we typically", "we tend to" };
        if (pastTenseIndicators.Any(p => lower.Contains(p)))
            score += 0.25;

        // Reference to previous similar situations
        var referenceWords = new[] { "similar", "same way", "last time", "previously", "before" };
        if (referenceWords.Any(r => lower.Contains(r)))
            score += 0.20;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 1: Calculate narrative signal (story and explanation requests).
    /// </summary>
    private double CalculateNarrativeSignal(string query)
    {
        double score = 0;
        var lower = query.ToLowerInvariant();

        // Narrative requests
        var narrativeRequests = new[] { "tell me", "explain", "describe", "walk me through", "what happened" };
        if (narrativeRequests.Any(n => lower.Contains(n)))
            score += 0.30;

        // Story elements
        var storyWords = new[] { "story", "happened", "then", "eventually", "first", "finally", "ended up" };
        if (storyWords.Any(s => lower.Contains(s)))
            score += 0.35;

        // Length (long messages often stories)
        if (query.Length > 150)
            score += 0.15;

        // Temporal progression words
        var temporal = new[] { "at first", "after that", "over time", "eventually", "meanwhile" };
        if (temporal.Any(t => lower.Contains(t)))
            score += 0.25;

        // Context about timing
        if (lower.Contains(" when ") || lower.Contains(" during ") || lower.Contains(" around the time"))
            score += 0.10;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 2: Detect negation in query.
    /// </summary>
    private bool HasNegation(string query)
    {
        var negations = new[] { "not", "don't", "doesn't", "didn't", "won't", "can't", "shouldn't", "no " };
        return negations.Any(n => Regex.IsMatch(query, @"\b" + Regex.Escape(n) + @"\b", RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Level 2: Calculate negation adjustment for signals.
    /// </summary>
    private double CalculateNegationAdjustment(QueryType type, bool hasNegation)
    {
        if (!hasNegation) return 0;

        return type switch
        {
            QueryType.FactRetrieval => 0.15,  // Negation strengthens retrieval (asking what NOT to do)
            QueryType.DeepRecall => -0.10,    // Negation weakens decision (less about choosing)
            QueryType.ProceduralTrigger => 0.10, // Negation somewhat helps patterns (avoiding past mistakes)
            _ => 0
        };
    }

    /// <summary>
    /// Level 2: Extract key domain nouns from query.
    /// </summary>
    private string[] ExtractKeyNouns(string query)
    {
        var domainNouns = new Dictionary<string, string[]>
        {
            ["database"] = new[] { "database", "sql", "postgres", "mongodb", "redis", "nosql", "db" },
            ["architecture"] = new[] { "architecture", "design", "system", "infrastructure", "microservice" },
            ["performance"] = new[] { "performance", "latency", "speed", "slow", "fast", "optimization" },
            ["security"] = new[] { "security", "encryption", "auth", "permission", "access control" },
            ["scaling"] = new[] { "scale", "scaled", "scaling", "growth", "load", "concurrent", "users" }
        };

        var lower = query.ToLowerInvariant();
        var foundDomains = new List<string>();

        foreach (var (domain, terms) in domainNouns)
        {
            if (terms.Any(t => lower.Contains(t)))
                foundDomains.Add(domain);
        }

        return foundDomains.ToArray();
    }

    /// <summary>
    /// Level 2: Calculate language intensity (emphasis indicators).
    /// </summary>
    private double CalculateLanguageIntensity(string query)
    {
        double intensity = 0;

        // All caps words (URGENT, CRITICAL)
        var words = query.Split(' ');
        var capsWords = words.Count(w => w.Length > 2 && w.All(char.IsUpper));
        intensity += Math.Min(capsWords * 0.15, 0.4);

        // Exclamation marks
        intensity += Math.Min(query.Count(c => c == '!') * 0.10, 0.3);

        // Multiple question marks
        intensity += Math.Min(query.Count(c => c == '?') * 0.05, 0.2);

        // Emphatic adverbs
        var emphatic = new[] { "very", "really", "absolutely", "definitely", "must", "critical", "urgent" };
        if (emphatic.Any(e => query.Contains(e, StringComparison.OrdinalIgnoreCase)))
            intensity += 0.15;

        return Math.Min(intensity, 1.0);
    }

    /// <summary>
    /// Level 3: Calculate contextual boost based on conversation state.
    /// </summary>
    private double CalculateContextualBoost(QueryContext context)
    {
        double boost = 0;

        // Early turns in conversation (context-setting phase)
        if (context.TurnNumber <= 3)
            boost += 0.15;

        // Topic consistency (if topics are present)
        if (context.CurrentTopics.Length > 0)
            boost += Math.Min(context.CurrentTopics.Length * 0.05, 0.20);

        return Math.Min(boost, 0.5);
    }

    /// <summary>
    /// Classifies query by signal scores and calculates confidence.
    /// </summary>
    private QueryClassification ClassifyBySignals(QuerySignals signals)
    {
        // Map QuerySignals to QueryType scores
        var scores = new Dictionary<QueryType, double>
        {
            [QueryType.FactRetrieval] = signals.RetrievalSignal,
            [QueryType.DeepRecall] = signals.NarrativeSignal,
            [QueryType.ProceduralTrigger] = signals.PatternSignal,
            [QueryType.Complex] = signals.DecisionSignal, // Decision queries often need complex analysis
            [QueryType.Continuation] = 0.0 // Continuation handled by quick classify
        };

        var total = scores.Values.Sum();

        // Avoid division by zero
        if (total == 0)
        {
            return new QueryClassification
            {
                Type = QueryType.Complex,
                Confidence = 0.0,
                AllScores = scores,
                Recommendation = ClassificationRecommendation.FallbackToAllLayers
            };
        }

        // Normalize to probability distribution
        var normalized = scores.ToDictionary(x => x.Key, x => x.Value / total);

        var bestType = normalized.MaxBy(x => x.Value).Key;
        var bestScore = normalized[bestType];

        var recommendation = bestScore switch
        {
            >= 0.80 => ClassificationRecommendation.UseSpecificLayers,
            >= 0.60 => ClassificationRecommendation.UseSpecificLayersWithFallback,
            _ => ClassificationRecommendation.FallbackToAllLayers
        };

        return new QueryClassification
        {
            Type = bestType,
            Confidence = bestScore,
            AllScores = normalized,
            Recommendation = recommendation
        };
    }

    /// <summary>
    /// Builds query plan from classification result with confidence-based layer selection.
    /// </summary>
    private QueryPlan BuildPlanFromClassification(QueryClassification classification, ConversationState state)
    {
        var plan = classification.Type switch
        {
            QueryType.FactRetrieval => BuildRetrievalPlan(classification, state),
            QueryType.DeepRecall => BuildNarrativePlan(classification, state),
            QueryType.ProceduralTrigger => BuildPatternPlan(classification, state),
            QueryType.Complex => BuildComplexPlan(classification, state),
            _ => CreatePlan(QueryType.Complex, state) // Fallback
        };

        return plan;
    }

    private QueryPlan BuildRetrievalPlan(QueryClassification c, ConversationState state)
    {
        return new QueryPlan
        {
            Type = c.Type,
            LayersToUse = c.Recommendation switch
            {
                ClassificationRecommendation.UseSpecificLayers =>
                    new List<MemoryLayer> { MemoryLayer.WorkingMemory, MemoryLayer.SemanticMemory },

                ClassificationRecommendation.UseSpecificLayersWithFallback =>
                    new List<MemoryLayer> { MemoryLayer.WorkingMemory, MemoryLayer.SemanticMemory, MemoryLayer.EpisodicMemory },

                _ => Enum.GetValues<MemoryLayer>().ToList()
            },
            EstimatedTokens = c.Confidence > 0.8 ? 500 : 1000
        };
    }

    private QueryPlan BuildNarrativePlan(QueryClassification c, ConversationState state)
    {
        return new QueryPlan
        {
            Type = c.Type,
            LayersToUse = c.Recommendation switch
            {
                ClassificationRecommendation.UseSpecificLayers =>
                    new List<MemoryLayer> { MemoryLayer.EpisodicMemory, MemoryLayer.WorkingMemory },

                _ => new List<MemoryLayer> 
                { 
                    MemoryLayer.EpisodicMemory, 
                    MemoryLayer.WorkingMemory, 
                    MemoryLayer.SemanticMemory 
                }
            },
            EstimatedTokens = c.Confidence > 0.8 ? 1500 : 2000
        };
    }

    private QueryPlan BuildPatternPlan(QueryClassification c, ConversationState state)
    {
        return new QueryPlan
        {
            Type = c.Type,
            LayersToUse = c.Recommendation switch
            {
                ClassificationRecommendation.UseSpecificLayers =>
                    new List<MemoryLayer> { MemoryLayer.ProceduralMemory, MemoryLayer.EpisodicMemory },

                _ => Enum.GetValues<MemoryLayer>().ToList()
            },
            EstimatedTokens = c.Confidence > 0.8 ? 300 : 800
        };
    }

    private QueryPlan BuildComplexPlan(QueryClassification c, ConversationState state)
    {
        return new QueryPlan
        {
            Type = c.Type,
            LayersToUse = Enum.GetValues<MemoryLayer>().ToList(),
            EstimatedTokens = 2000
        };
    }

    #endregion

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

/// <summary>
/// Represents signal scores for query classification.
/// </summary>
public record QuerySignals
{
    public double RetrievalSignal { get; init; }
    public double DecisionSignal { get; init; }
    public double PatternSignal { get; init; }
    public double NarrativeSignal { get; init; }
}

/// <summary>
/// Represents a query classification result with confidence metrics.
/// </summary>
public record QueryClassification
{
    public required QueryType Type { get; init; }
    public required double Confidence { get; init; }
    public required Dictionary<QueryType, double> AllScores { get; init; }
    public required ClassificationRecommendation Recommendation { get; init; }
}

/// <summary>
/// Recommendation for layer usage based on classification confidence.
/// </summary>
public enum ClassificationRecommendation
{
    /// <summary>Use only specific layers (high confidence >= 0.80)</summary>
    UseSpecificLayers,
    
    /// <summary>Use specific layers with fallback (medium confidence 0.60-0.80)</summary>
    UseSpecificLayersWithFallback,
    
    /// <summary>Use all layers (low confidence &lt; 0.60)</summary>
    FallbackToAllLayers
}

/// <summary>
/// Context for query classification including conversation history.
/// </summary>
public record QueryContext
{
    public required string CurrentQuery { get; init; }
    public Domain.Entities.Message[] RecentHistory { get; init; } = Array.Empty<Domain.Entities.Message>();
    public string[] CurrentTopics { get; init; } = Array.Empty<string>();
    public int TurnNumber { get; init; }
}
