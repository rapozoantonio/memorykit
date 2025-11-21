using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Cognitive;

/// <summary>
/// Enhanced Amygdala Importance Engine - wraps base implementation with LLM-powered sentiment analysis.
/// Uses Decorator pattern to add LLM capabilities while reusing core heuristic logic.
/// </summary>
public class AmygdalaImportanceEngineService : IAmygdalaImportanceEngine
{
    private readonly AmygdalaImportanceEngine _baseEngine;
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<AmygdalaImportanceEngineService> _logger;

    public AmygdalaImportanceEngineService(
        AmygdalaImportanceEngine baseEngine,
        ISemanticKernelService llm,
        ILogger<AmygdalaImportanceEngineService> logger)
    {
        _baseEngine = baseEngine;
        _llm = llm;
        _logger = logger;
    }

    public async Task<ImportanceScore> CalculateImportanceAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Calculating enhanced importance for message {MessageId}",
            message.Id);

        // Get base scores from heuristic engine
        var baseScore = _baseEngine.CalculateBaseScore(message);
        var noveltyBoost = _baseEngine.CalculateNoveltyBoost(message);
        var recencyFactor = _baseEngine.CalculateRecencyFactor(message);

        // Enhance emotional weight with LLM sentiment analysis
        var emotionalWeight = await CalculateEnhancedEmotionalWeightAsync(
            message,
            cancellationToken);

        var score = new ImportanceScore
        {
            BaseScore = baseScore,
            EmotionalWeight = emotionalWeight,
            NoveltyBoost = noveltyBoost,
            RecencyFactor = recencyFactor
        };

        _logger.LogDebug(
            "Enhanced importance score: Base={Base:F2}, Emotional={Emotional:F2}, " +
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
        // Delegate to base engine
        return _baseEngine.CalculateEntityImportanceAsync(entityKey, entityValue, cancellationToken);
    }

    public bool ContainsDecisionLanguage(string text)
    {
        // Delegate to base engine
        return _baseEngine.ContainsDecisionLanguage(text);
    }

    public bool HasExplicitImportanceMarkers(string text)
    {
        // Delegate to base engine
        return _baseEngine.HasExplicitImportanceMarkers(text);
    }

    /// <summary>
    /// Calculates enhanced emotional weight combining heuristics and LLM sentiment analysis.
    /// Falls back to base heuristics if LLM fails.
    /// </summary>
    private async Task<double> CalculateEnhancedEmotionalWeightAsync(
        Message message,
        CancellationToken cancellationToken)
    {
        // Start with base heuristic emotional weight
        var baseEmotionalWeight = _baseEngine.CalculateEmotionalWeight(message);

        try
        {
            // Enhance with LLM sentiment analysis
            var sentiment = await AnalyzeSentimentAsync(message.Content, cancellationToken);

            // High absolute sentiment = high importance (Range: -1 to +1, we want 0 to 1)
            var llmEmotionalWeight = Math.Abs(sentiment.Score) * 0.5;

            // Use the maximum of heuristic and LLM scores for best accuracy
            return Math.Max(baseEmotionalWeight, llmEmotionalWeight);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "LLM sentiment analysis failed, using base heuristic emotional weight");

            // Graceful degradation: return base heuristic score
            return baseEmotionalWeight;
        }
    }
}
