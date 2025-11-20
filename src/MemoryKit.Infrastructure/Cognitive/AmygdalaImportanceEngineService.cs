using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Cognitive;

/// <summary>
/// Amygdala Importance Engine - emotional tagging and importance scoring.
/// Implements TRD Section 6.3.
/// </summary>
public class AmygdalaImportanceEngineService : IAmygdalaImportanceEngine
{
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<AmygdalaImportanceEngineService> _logger;

    private static readonly string[] DecisionPatterns = new[]
    {
        "i will", "let's", "we should", "i decided", "going to",
        "plan to", "commit to", "promise", "agree to"
    };

    private static readonly string[] ImportanceMarkers = new[]
    {
        "important", "critical", "remember", "don't forget",
        "always", "never", "from now on", "note that", "key",
        "crucial", "essential", "vital"
    };

    public AmygdalaImportanceEngineService(
        ISemanticKernelService llm,
        ILogger<AmygdalaImportanceEngineService> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    public async Task<ImportanceScore> CalculateImportanceAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Calculating importance for message {MessageId}",
            message.Id);

        var baseScore = CalculateBaseScore(message);
        var emotionalWeight = await CalculateEmotionalWeightAsync(
            message.Content,
            cancellationToken);
        var noveltyBoost = CalculateNoveltyBoost(message);
        var recencyFactor = CalculateRecencyFactor(message);

        var score = new ImportanceScore
        {
            BaseScore = baseScore,
            EmotionalWeight = emotionalWeight,
            NoveltyBoost = noveltyBoost,
            RecencyFactor = recencyFactor
        };

        _logger.LogDebug(
            "Importance score: Base={Base:F2}, Emotional={Emotional:F2}, " +
            "Novelty={Novelty:F2}, Recency={Recency:F2}, Final={Final:F2}",
            score.BaseScore,
            score.EmotionalWeight,
            score.NoveltyBoost,
            score.RecencyFactor,
            score.FinalScore);

        return score;
    }

    public async Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _llm.AnalyzeSentimentAsync(text, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Sentiment analysis failed, returning neutral");

            return (0.0, "Neutral");
        }
    }

    public Task<double> CalculateEntityImportanceAsync(
        string entityKey,
        string entityValue,
        CancellationToken cancellationToken = default)
    {
        double score = 0.5;

        // Longer values are often more important
        if (entityValue.Length > 100)
            score += 0.2;

        // Technical terms and proper nouns
        if (entityValue.Length > 0 && char.IsUpper(entityValue[0]))
            score += 0.1;

        return Task.FromResult(Math.Min(score, 1.0));
    }

    public bool ContainsDecisionLanguage(string text)
    {
        var lower = text.ToLowerInvariant();
        return DecisionPatterns.Any(pattern =>
            lower.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasExplicitImportanceMarkers(string text)
    {
        var lower = text.ToLowerInvariant();
        return ImportanceMarkers.Any(marker =>
            lower.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Calculates base importance score using heuristics.
    /// </summary>
    private double CalculateBaseScore(Message message)
    {
        double score = 0.5; // Baseline

        // Question detection (user asking for information)
        if (message.Content.Contains('?'))
        {
            score += 0.2;
        }

        // Decision language
        if (ContainsDecisionLanguage(message.Content))
        {
            score += 0.3;
        }

        // Explicit importance markers
        if (HasExplicitImportanceMarkers(message.Content))
        {
            score += 0.5;
        }

        // Code blocks (technical importance)
        if (message.Content.Contains("```") || message.Content.Contains("```"))
        {
            score += 0.15;
        }

        // Long messages are often more important
        if (message.Content.Length > 500)
        {
            score += 0.1;
        }

        // User messages are generally more important than assistant
        if (message.Role == Domain.Enums.MessageRole.User)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    /// <summary>
    /// Calculates emotional weight using sentiment analysis.
    /// </summary>
    private async Task<double> CalculateEmotionalWeightAsync(
        string content,
        CancellationToken cancellationToken)
    {
        try
        {
            var sentiment = await AnalyzeSentimentAsync(content, cancellationToken);

            // High absolute sentiment = high importance
            // Range: -1 to +1, we want 0 to 1
            return Math.Abs(sentiment.Score) * 0.5;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to calculate emotional weight, using default");

            return 0.0;
        }
    }

    /// <summary>
    /// Calculates novelty boost based on new entities.
    /// </summary>
    private double CalculateNoveltyBoost(Message message)
    {
        // Check if message introduces new entities
        var newEntityCount = message.Metadata.ExtractedEntities?
            .Count(e => e.IsNovel) ?? 0;

        return Math.Min(newEntityCount * 0.1, 0.5);
    }

    /// <summary>
    /// Calculates recency factor with exponential decay.
    /// Importance decreases over time.
    /// </summary>
    private double CalculateRecencyFactor(Message message)
    {
        var age = DateTime.UtcNow - message.Timestamp;

        // Exponential decay: importance halves every 24 hours
        // After 3 days, recency factor is ~12.5% of original
        return Math.Exp(-age.TotalHours / 24.0);
    }
}
