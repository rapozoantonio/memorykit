using MemoryKit.Application.Services;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Cognitive;

/// <summary>
/// Enhanced Prefrontal Controller - wraps base implementation with LLM-powered classification.
/// Uses Decorator pattern to add LLM capabilities while reusing core pattern-matching logic.
/// </summary>
public class PrefrontalControllerService : IPrefrontalController
{
    private readonly PrefrontalController _baseController;
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<PrefrontalControllerService> _logger;

    public PrefrontalControllerService(
        PrefrontalController baseController,
        ISemanticKernelService llm,
        ILogger<PrefrontalControllerService> logger)
    {
        _baseController = baseController;
        _llm = llm;
        _logger = logger;
    }

    public async Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Building enhanced query plan for: {Query}", query);

        // Try base pattern-based classification first
        var quickType = _baseController.QuickClassify(query);

        QueryType classifiedType;
        if (quickType.HasValue)
        {
            classifiedType = quickType.Value;
            _logger.LogDebug(
                "Pattern-based classification: {Type}",
                classifiedType);
        }
        else
        {
            // Use LLM for complex queries that patterns couldn't classify
            classifiedType = await ClassifyQueryWithLlmAsync(query, cancellationToken);
            _logger.LogDebug(
                "LLM-enhanced classification: {Type}",
                classifiedType);
        }

        // Reuse base controller's plan creation logic
        var plan = _baseController.CreatePlan(classifiedType, state);

        _logger.LogInformation(
            "Enhanced query plan created: Type={Type}, Layers={LayerCount}",
            plan.Type,
            plan.LayersToUse.Count);

        return plan;
    }

    public async Task<QueryType> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Delegate to base controller for pattern-based classification first
        var quickType = _baseController.QuickClassify(query);
        if (quickType.HasValue)
        {
            return quickType.Value;
        }

        // Fall back to LLM for complex classification
        return await ClassifyQueryWithLlmAsync(query, cancellationToken);
    }

    /// <summary>
    /// Use LLM to classify queries that pattern-matching couldn't handle.
    /// </summary>
    private async Task<QueryType> ClassifyQueryWithLlmAsync(
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
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
        // Delegate to base controller
        return _baseController.DetermineLayersToUse(queryType, state);
    }
}
