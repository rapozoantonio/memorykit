using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace MemoryKit.Application.Services;

/// <summary>
/// Amygdala analog: Emotional tagging and importance scoring.
/// Calculates the importance of messages for memory consolidation.
/// </summary>
public class AmygdalaImportanceEngine : IAmygdalaImportanceEngine
{
    private readonly ILogger<AmygdalaImportanceEngine> _logger;

    public AmygdalaImportanceEngine(ILogger<AmygdalaImportanceEngine> logger)
    {
        _logger = logger;
    }

    public Task<ImportanceScore> CalculateImportanceAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        // Calculate all signal components
        var components = new ImportanceSignalComponents
        {
            DecisionLanguageScore = CalculateDecisionLanguageScore(message.Content),
            ExplicitImportanceScore = CalculateExplicitMarkerScore(message.Content),
            QuestionScore = CalculateQuestionScore(message.Content),
            CodeBlockScore = CalculateCodeBlockScore(message.Content),
            NoveltyScore = CalculateNoveltyScoreEnhanced(message),
            SentimentScore = CalculateSentimentScore(message.Content),
            TechnicalDepthScore = CalculateTechnicalDepthScore(message.Content),
            ConversationContextScore = CalculateConversationContextScore(message)
        };

        // Calculate final score using geometric mean (more robust than simple weighted sum)
        var finalScore = CalculateFinalScoreFromComponents(components);

        // Map to existing ImportanceScore structure for backward compatibility
        var importance = new ImportanceScore
        {
            BaseScore = (components.DecisionLanguageScore + components.ExplicitImportanceScore + components.QuestionScore + components.CodeBlockScore) / 4.0,
            EmotionalWeight = components.SentimentScore,
            NoveltyBoost = components.NoveltyScore,
            RecencyFactor = CalculateRecencyFactor(message)
        };

        _logger.LogDebug(
            "Calculated importance for message {MessageId}: Final={Final:F3}, " +
            "Components=[Decision={Decision:F2}, Explicit={Explicit:F2}, Question={Question:F2}, " +
            "Code={Code:F2}, Novelty={Novelty:F2}, Sentiment={Sentiment:F2}, " +
            "Technical={Technical:F2}, Context={Context:F2}]",
            message.Id,
            finalScore,
            components.DecisionLanguageScore,
            components.ExplicitImportanceScore,
            components.QuestionScore,
            components.CodeBlockScore,
            components.NoveltyScore,
            components.SentimentScore,
            components.TechnicalDepthScore,
            components.ConversationContextScore);

        return Task.FromResult(importance);
    }

    public Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
        // Simplified sentiment analysis - production would use Azure AI
        var score = CalculateEmotionalWeight(
            Message.Create("system", "temp", Domain.Enums.MessageRole.System, text));
        var sentiment = score > 0.7 ? "Positive" : score < 0.3 ? "Negative" : "Neutral";
        return Task.FromResult((score, sentiment));
    }

    public bool ContainsDecisionLanguage(string text)
    {
        return DecisionPatterns.Any(p =>
            text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasExplicitImportanceMarkers(string text)
    {
        return ImportanceMarkers.Any(m =>
            text.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    public Task<double> CalculateEntityImportanceAsync(
        string entityKey,
        string entityValue,
        CancellationToken cancellationToken = default)
    {
        // Base importance score for entities
        double score = 0.5;

        // Longer values are often more important (descriptions, explanations)
        if (entityValue.Length > 100)
            score += 0.2;

        // Technical terms and proper nouns are important
        if (char.IsUpper(entityValue[0]))
            score += 0.1;

        return Task.FromResult(Math.Min(score, 1.0));
    }

    /// <summary>
    /// Calculates base importance score based on content patterns.
    /// </summary>
    public virtual double CalculateBaseScore(Message message)
    {
        double score = 0.5; // Baseline

        // Question detection
        if (message.Content.Contains('?'))
            score += 0.2;

        // Decision language
        if (DecisionPatterns.Any(p =>
            message.Content.Contains(p, StringComparison.OrdinalIgnoreCase)))
            score += 0.3;

        // Explicit importance markers
        if (ImportanceMarkers.Any(m =>
            message.Content.Contains(m, StringComparison.OrdinalIgnoreCase)))
            score += 0.5;

        // Code blocks (technical importance)
        if (message.Content.Contains("```") || message.Content.Contains("code"))
            score += 0.15;

        // Long messages tend to be more important
        if (message.Content.Length > 500)
            score += 0.1;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculates emotional weight based on sentiment markers.
    /// This is a simplified version - production would use Azure AI sentiment analysis.
    /// </summary>
    public virtual double CalculateEmotionalWeight(Message message)
    {
        var content = message.Content.ToLowerInvariant();
        double emotionalScore = 0.0;

        // Positive emotion markers
        var positiveCount = PositiveMarkers.Count(m => content.Contains(m));
        emotionalScore += positiveCount * 0.1;

        // Negative emotion markers
        var negativeCount = NegativeMarkers.Count(m => content.Contains(m));
        emotionalScore += negativeCount * 0.15; // Negative emotions weighted higher

        // Exclamation marks indicate emotional content
        emotionalScore += Math.Min(content.Count(c => c == '!') * 0.05, 0.2);

        // ALL CAPS words indicate strong emotion
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var capsWords = words.Count(w => w.Length > 3 && w.All(char.IsUpper));
        emotionalScore += Math.Min(capsWords * 0.1, 0.3);

        return Math.Min(emotionalScore, 1.0);
    }

    /// <summary>
    /// Calculates novelty boost based on new information.
    /// Simplified version - production would use embeddings to detect truly novel content.
    /// </summary>
    public virtual double CalculateNoveltyBoost(Message message)
    {
        // Check if message introduces new entities (simplified)
        var newEntityCount = message.Metadata.ExtractedEntities?
            .Count(e => e.IsNovel) ?? 0;

        var boost = Math.Min(newEntityCount * 0.15, 0.5);

        // First message in conversation is novel
        if (message.Metadata.Tags?.Contains("first_message") == true)
            boost += 0.3;

        return boost;
    }

    /// <summary>
    /// Calculates recency factor with exponential decay.
    /// </summary>
    public virtual double CalculateRecencyFactor(Message message)
    {
        var age = DateTime.UtcNow - message.Timestamp;

        // Exponential decay: importance halves every 24 hours
        // Fresh messages (< 1 hour) get full recency score
        if (age.TotalHours < 1)
            return 1.0;

        return Math.Exp(-age.TotalHours / 24.0);
    }

    #region Enhanced Signal Calculation Methods

    /// <summary>
    /// Level 1: Calculate decision language score with optimized single-pass matching.
    /// </summary>
    private double CalculateDecisionLanguageScore(string content)
    {
        var lower = content.ToLowerInvariant();

        // Single pass through decision patterns with weights
        double score = 0;
        foreach (var (pattern, weight) in DecisionPatternsWeighted)
        {
            if (lower.Contains(pattern))
            {
                score = Math.Max(score, weight); // Take highest matching weight
            }
        }

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 1: Calculate explicit importance marker score with optimized single-pass matching.
    /// </summary>
    private double CalculateExplicitMarkerScore(string content)
    {
        var lower = content.ToLowerInvariant();

        // Single pass through importance markers with weights
        double score = 0;
        foreach (var (marker, weight) in ImportanceMarkersWeighted)
        {
            if (lower.Contains(marker))
            {
                score = Math.Max(score, weight); // Take highest matching weight
            }
        }

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 1: Calculate question score.
    /// </summary>
    private double CalculateQuestionScore(string content)
    {
        if (!content.TrimEnd().EndsWith("?"))
            return 0.05; // Slight boost for clarifying statements

        // Decision-oriented questions are more important
        if (Regex.IsMatch(content, @"(should|must|will|can|could|may)\s", RegexOptions.IgnoreCase))
            return 0.40;

        // Factual questions are moderately important
        return 0.20;
    }

    /// <summary>
    /// Level 1: Calculate code block score with optimized single-pass matching.
    /// </summary>
    private double CalculateCodeBlockScore(string content)
    {
        // Code blocks are almost always important (check first)
        if (content.Contains("```"))
            return 0.60;

        // Inline code
        if (content.Contains('`') && Regex.IsMatch(content, @"`[^`]+`"))
            return 0.45;

        // Code-related keywords - single pass
        var lower = content.ToLowerInvariant();
        foreach (var keyword in CodeKeywords)
        {
            if (lower.Contains(keyword))
                return 0.30;
        }

        return 0;
    }

    /// <summary>
    /// Level 2: Calculate enhanced novelty score.
    /// </summary>
    private double CalculateNoveltyScoreEnhanced(Message message)
    {
        double score = 0;

        // New entities mentioned boost importance
        var newEntities = message.Metadata.ExtractedEntities?
            .Count(e => e.IsNovel) ?? 0;

        score += Math.Min(newEntities * 0.15, 0.50);

        // First message in conversation
        if (message.Metadata.Tags?.Contains("first_message") == true)
            score += 0.30;

        // New technical terms (capitalized words that aren't common)
        var words = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var capitalizedWords = words.Count(w => w.Length > 3 && char.IsUpper(w[0]) && !CommonWords.Contains(w.ToLowerInvariant()));
        score += Math.Min(capitalizedWords * 0.05, 0.20);

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 2: Calculate sentiment score.
    /// </summary>
    private double CalculateSentimentScore(string content)
    {
        double score = 0;
        var lower = content.ToLowerInvariant();

        // Strong positive sentiment
        var positive = new[] { "excellent", "perfect", "amazing", "great", "best practice", "optimal", "ideal" };
        if (positive.Any(p => lower.Contains(p)))
            score += 0.25;

        // Strong negative sentiment (problems are important to remember)
        var negative = new[] { "problem", "issue", "critical", "bug", "broken", "emergency", "failure", "error" };
        if (negative.Any(n => lower.Contains(n)))
            score += 0.35;

        // Exclamation marks indicate emotional content
        score += Math.Min(content.Count(c => c == '!') * 0.05, 0.15);

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 2: Calculate technical depth score.
    /// </summary>
    private double CalculateTechnicalDepthScore(string content)
    {
        double score = 0;

        // Technical vocabulary
        var technical = new[] { "algorithm", "architecture", "optimization", "refactor", "api", "protocol", "infrastructure", "scalability" };
        var technicalCount = technical.Count(t => content.Contains(t, StringComparison.OrdinalIgnoreCase));
        score += Math.Min(technicalCount * 0.15, 0.40);

        // Length > 200 chars suggests detailed explanation
        if (content.Length > 200)
            score += 0.15;

        // Technical acronyms (all caps words 2-6 chars)
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var acronyms = words.Count(w => w.Length >= 2 && w.Length <= 6 && w.All(char.IsUpper));
        score += Math.Min(acronyms * 0.10, 0.20);

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Level 3: Calculate conversation context score.
    /// </summary>
    private double CalculateConversationContextScore(Message message)
    {
        double score = 0;

        // First 3 messages of conversation are context-setting, important
        if (message.Metadata.Tags?.Contains("early_conversation") == true)
            score += 0.15;

        // Messages that reference previous decisions
        if (Regex.IsMatch(message.Content, @"(as we discussed|as I mentioned|previously|before|earlier)"))
            score += 0.25;

        // Messages that set context for future
        if (Regex.IsMatch(message.Content, @"(from now on|going forward|in the future|remember that)"))
            score += 0.20;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculate final importance score using geometric mean for robustness.
    /// </summary>
    private double CalculateFinalScoreFromComponents(ImportanceSignalComponents components)
    {
        // Collect all non-zero scores
        var scores = new[]
        {
            components.DecisionLanguageScore,
            components.ExplicitImportanceScore,
            components.QuestionScore,
            components.CodeBlockScore,
            components.NoveltyScore,
            components.SentimentScore,
            components.TechnicalDepthScore,
            components.ConversationContextScore
        };

        var nonZeroScores = scores.Where(s => s > 0.01).ToArray(); // Threshold to avoid log(0)

        if (nonZeroScores.Length == 0)
            return 0.3; // Default low importance if no signals

        // Geometric mean (more robust than arithmetic mean)
        var product = nonZeroScores.Aggregate(1.0, (a, b) => a * b);
        var geometricMean = Math.Pow(product, 1.0 / nonZeroScores.Length);

        // Apply dampening factor to avoid over-scoring
        var dampened = geometricMean * 0.90;

        return Math.Min(dampened, 1.0);
    }

    #endregion

    // Common words to exclude from novelty detection
    private static readonly HashSet<string> CommonWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "this", "that", "from", "have", "been",
        "will", "would", "could", "should", "about", "which", "their", "there"
    };

    // Pattern definitions
    private static readonly string[] DecisionPatterns = new[]
    {
        "i will", "let's", "we should", "i decided", "going to",
        "plan to", "commit to", "i'll", "we'll", "must"
    };

    private static readonly string[] ImportanceMarkers = new[]
    {
        "important", "critical", "remember", "don't forget", "note that",
        "always", "never", "from now on", "crucial", "essential",
        "key point", "take note"
    };

    private static readonly string[] PositiveMarkers = new[]
    {
        "great", "excellent", "perfect", "amazing", "wonderful",
        "fantastic", "awesome", "love", "thank you", "thanks"
    };

    private static readonly string[] NegativeMarkers = new[]
    {
        "problem", "issue", "error", "bug", "fail", "wrong",
        "broken", "crash", "urgent", "critical", "emergency"
    };

    // Optimized weighted patterns for single-pass scoring
    private static readonly (string Pattern, double Weight)[] DecisionPatternsWeighted = new[]
    {
        ("decided", 0.50), ("decided to", 0.50), ("committed", 0.50),
        ("will commit", 0.50), ("final decision", 0.50), ("i choose", 0.50),
        (" will ", 0.25), ("going to", 0.25), ("plan to", 0.25),
        ("consider", 0.15), ("thinking about", 0.15), ("maybe", 0.15),
        ("might", 0.15), ("considering", 0.15)
    };

    private static readonly (string Marker, double Weight)[] ImportanceMarkersWeighted = new[]
    {
        ("critical", 0.60), ("crucial", 0.60), ("essential", 0.60),
        ("must", 0.60), ("required", 0.60), ("vital", 0.60),
        ("important", 0.40), ("remember", 0.40), ("note that", 0.40),
        ("key point", 0.40), ("significant", 0.40),
        ("don't forget", 0.35), ("important to note", 0.35),
        ("remember to", 0.35), ("take note", 0.35), ("pay attention", 0.35)
    };

    private static readonly string[] CodeKeywords = new[]
    {
        "function", "class", "method", "algorithm", "implementation"
    };
}

/// <summary>
/// Represents detailed signal components for importance scoring.
/// </summary>
public record ImportanceSignalComponents
{
    public double DecisionLanguageScore { get; init; }
    public double ExplicitImportanceScore { get; init; }
    public double QuestionScore { get; init; }
    public double CodeBlockScore { get; init; }
    public double NoveltyScore { get; init; }
    public double SentimentScore { get; init; }
    public double TechnicalDepthScore { get; init; }
    public double ConversationContextScore { get; init; }
}
