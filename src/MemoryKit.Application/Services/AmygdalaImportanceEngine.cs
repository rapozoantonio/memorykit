using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;

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
        var baseScore = CalculateBaseScore(message);
        var emotionalWeight = CalculateEmotionalWeight(message);
        var noveltyBoost = CalculateNoveltyBoost(message);
        var recencyFactor = CalculateRecencyFactor(message);

        var importance = new ImportanceScore
        {
            BaseScore = baseScore,
            EmotionalWeight = emotionalWeight,
            NoveltyBoost = noveltyBoost,
            RecencyFactor = recencyFactor
        };

        _logger.LogDebug(
            "Calculated importance for message {MessageId}: Final={Final:F3}, Base={Base:F3}, Emotional={Emotional:F3}, Novelty={Novelty:F3}, Recency={Recency:F3}",
            message.Id,
            importance.FinalScore,
            baseScore,
            emotionalWeight,
            noveltyBoost,
            recencyFactor);

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
    private double CalculateBaseScore(Message message)
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
    private double CalculateEmotionalWeight(Message message)
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
    private double CalculateNoveltyBoost(Message message)
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
    private double CalculateRecencyFactor(Message message)
    {
        var age = DateTime.UtcNow - message.Timestamp;

        // Exponential decay: importance halves every 24 hours
        // Fresh messages (< 1 hour) get full recency score
        if (age.TotalHours < 1)
            return 1.0;

        return Math.Exp(-age.TotalHours / 24.0);
    }

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
}
