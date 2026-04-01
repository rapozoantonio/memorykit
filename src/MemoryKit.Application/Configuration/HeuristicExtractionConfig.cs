namespace MemoryKit.Application.Configuration;

/// <summary>
/// Configuration for heuristic-based semantic fact extraction.
/// Enables extraction without AI/LLM dependencies for offline deployments
/// and cost optimization scenarios.
/// </summary>
public class HeuristicExtractionConfig
{
    /// <summary>
    /// If true, attempt heuristic extraction first before calling LLM.
    /// If false, always use LLM extraction (original behavior).
    /// Default: true (smart hybrid mode)
    /// </summary>
    public bool UseHeuristicFirst { get; set; } = true;

    /// <summary>
    /// If true, ONLY use heuristic extraction and never call LLM.
    /// Takes precedence over UseHeuristicFirst.
    /// Useful for offline/air-gapped deployments or zero-cost extraction.
    /// Default: false
    /// </summary>
    public bool HeuristicOnly { get; set; }

    /// <summary>
    /// Minimum number of facts that heuristic extraction must produce
    /// before skipping LLM extraction.
    /// Only applies when UseHeuristicFirst=true and HeuristicOnly=false.
    /// Default: 2
    /// </summary>
    public int MinHeuristicFactsForAI { get; set; } = 2;

    /// <summary>
    /// If true, log which extraction method was used for each message
    /// (heuristic-only, heuristic-sufficient, heuristic-plus-llm, llm-only).
    /// Useful for monitoring and optimization.
    /// Default: true
    /// </summary>
    public bool LogExtractionMethod { get; set; } = true;

    /// <summary>
    /// If true, extract narrative fragments when structured extraction yields zero facts.
    /// Enables semantic search even without structured facts by storing cleaned message text.
    /// Default: true (graceful degradation)
    /// </summary>
    public bool UseNarrativeFallback { get; set; } = true;

    /// <summary>
    /// Maximum number of narrative fragments to extract per message.
    /// Limits storage and prevents noise in semantic memory.
    /// Default: 3
    /// </summary>
    public int MaxNarrativeFragmentsPerMessage { get; set; } = 3;

    /// <summary>
    /// Importance score assigned to narrative fragments.
    /// Lower than structured facts (0.65-0.85) to prioritize verified knowledge.
    /// Default: 0.50 (medium-low priority)
    /// </summary>
    public double NarrativeImportanceScore { get; set; } = 0.50;
}
