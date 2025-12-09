namespace MemoryKit.Application.Configuration;

/// <summary>
/// Configuration for smart heuristics query classification and importance scoring.
/// </summary>
public class SmartHeuristicsConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "MemoryKit:SmartHeuristics";

    /// <summary>
    /// Gets or sets the query classification configuration.
    /// </summary>
    public QueryClassificationConfig QueryClassification { get; set; } = new();

    /// <summary>
    /// Gets or sets the importance scoring configuration.
    /// </summary>
    public ImportanceScoringConfig ImportanceScoring { get; set; } = new();
}

/// <summary>
/// Configuration for query classification signals and thresholds.
/// </summary>
public class QueryClassificationConfig
{
    /// <summary>
    /// Gets or sets the confidence threshold for using specific layers (default: 0.80).
    /// </summary>
    public double SpecificLayersThreshold { get; set; } = 0.80;

    /// <summary>
    /// Gets or sets the confidence threshold for using layers with fallback (default: 0.60).
    /// </summary>
    public double WithFallbackThreshold { get; set; } = 0.60;

    /// <summary>
    /// Gets or sets the signal weights for query classification.
    /// </summary>
    public SignalWeights SignalWeights { get; set; } = new();

    /// <summary>
    /// Gets or sets custom pattern dictionaries for classification.
    /// </summary>
    public PatternDictionaries Patterns { get; set; } = new();
}

/// <summary>
/// Signal weights for query classification (0.0 to 2.0, default 1.0).
/// </summary>
public class SignalWeights
{
    /// <summary>
    /// Weight for surface-level pattern matching signals (default: 1.0).
    /// </summary>
    public double SurfaceSignalWeight { get; set; } = 1.0;

    /// <summary>
    /// Weight for semantic analysis signals (default: 1.2).
    /// </summary>
    public double SemanticSignalWeight { get; set; } = 1.2;

    /// <summary>
    /// Weight for contextual conversation signals (default: 0.8).
    /// </summary>
    public double ContextualSignalWeight { get; set; } = 0.8;

    /// <summary>
    /// Weight for negation adjustments (default: 1.0).
    /// </summary>
    public double NegationWeight { get; set; } = 1.0;

    /// <summary>
    /// Weight for intensity boosts (default: 1.0).
    /// </summary>
    public double IntensityWeight { get; set; } = 1.0;
}

/// <summary>
/// Custom pattern dictionaries for query classification.
/// </summary>
public class PatternDictionaries
{
    /// <summary>
    /// Additional continuation patterns (beyond defaults).
    /// </summary>
    public string[] ContinuationPatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Additional fact retrieval patterns (beyond defaults).
    /// </summary>
    public string[] FactRetrievalPatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Additional deep recall patterns (beyond defaults).
    /// </summary>
    public string[] DeepRecallPatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Additional procedural trigger patterns (beyond defaults).
    /// </summary>
    public string[] ProceduralTriggerPatterns { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Configuration for importance scoring components and weights.
/// </summary>
public class ImportanceScoringConfig
{
    /// <summary>
    /// Gets or sets the component weights for importance calculation.
    /// </summary>
    public ImportanceComponentWeights ComponentWeights { get; set; } = new();

    /// <summary>
    /// Gets or sets the dampening factor for final score (0.0 to 1.0, default: 0.90).
    /// Prevents over-scoring by reducing the geometric mean.
    /// </summary>
    public double DampeningFactor { get; set; } = 0.90;

    /// <summary>
    /// Gets or sets the default importance score for messages with no signals (0.0 to 1.0, default: 0.30).
    /// </summary>
    public double DefaultScore { get; set; } = 0.30;

    /// <summary>
    /// Gets or sets importance thresholds for categorization.
    /// </summary>
    public ImportanceThresholds Thresholds { get; set; } = new();
}

/// <summary>
/// Component weights for importance scoring (0.0 to 2.0, default 1.0).
/// Higher values increase the influence of that component.
/// </summary>
public class ImportanceComponentWeights
{
    /// <summary>
    /// Weight for decision language detection (default: 1.2).
    /// </summary>
    public double DecisionLanguageWeight { get; set; } = 1.2;

    /// <summary>
    /// Weight for explicit importance markers (default: 1.5).
    /// </summary>
    public double ExplicitMarkerWeight { get; set; } = 1.5;

    /// <summary>
    /// Weight for question scoring (default: 0.8).
    /// </summary>
    public double QuestionWeight { get; set; } = 0.8;

    /// <summary>
    /// Weight for code block detection (default: 1.3).
    /// </summary>
    public double CodeBlockWeight { get; set; } = 1.3;

    /// <summary>
    /// Weight for novelty detection (default: 1.0).
    /// </summary>
    public double NoveltyWeight { get; set; } = 1.0;

    /// <summary>
    /// Weight for sentiment analysis (default: 0.9).
    /// </summary>
    public double SentimentWeight { get; set; } = 0.9;

    /// <summary>
    /// Weight for technical depth analysis (default: 1.1).
    /// </summary>
    public double TechnicalDepthWeight { get; set; } = 1.1;

    /// <summary>
    /// Weight for conversation context (default: 1.0).
    /// </summary>
    public double ConversationContextWeight { get; set; } = 1.0;
}

/// <summary>
/// Thresholds for importance categorization.
/// </summary>
public class ImportanceThresholds
{
    /// <summary>
    /// Threshold for critical importance (default: 0.80).
    /// </summary>
    public double CriticalThreshold { get; set; } = 0.80;

    /// <summary>
    /// Threshold for high importance (default: 0.60).
    /// </summary>
    public double HighThreshold { get; set; } = 0.60;

    /// <summary>
    /// Threshold for normal importance (default: 0.40).
    /// </summary>
    public double NormalThreshold { get; set; } = 0.40;
}
