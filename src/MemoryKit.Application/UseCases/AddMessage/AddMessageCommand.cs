using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.Configuration;
using MemoryKit.Application.DTOs;
using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Application.UseCases.AddMessage;

/// <summary>
/// Command to add a message to a conversation.
/// </summary>
public record AddMessageCommand(
    string UserId,
    string ConversationId,
    CreateMessageRequest Request) : IRequest<MessageResponse>;

/// <summary>
/// Handler for AddMessageCommand.
/// </summary>
public class AddMessageHandler : IRequestHandler<AddMessageCommand, MessageResponse>
{
    private readonly IMemoryOrchestrator _orchestrator;
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<AddMessageHandler> _logger;
    private readonly HeuristicExtractionConfig _heuristicConfig;

    public AddMessageHandler(
        IMemoryOrchestrator orchestrator,
        ISemanticKernelService llm,
        ILogger<AddMessageHandler> logger,
        IConfiguration configuration)
    {
        _orchestrator = orchestrator;
        _llm = llm;
        _logger = logger;

        // Read heuristic extraction configuration
        _heuristicConfig = new HeuristicExtractionConfig();
        configuration.GetSection("MemoryKit:HeuristicExtraction").Bind(_heuristicConfig);
    }

    public async Task<MessageResponse> Handle(
        AddMessageCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding message to conversation {ConversationId} for user {UserId}",
            request.ConversationId,
            request.UserId);

        // Create message entity
        var message = Message.Create(
            request.UserId,
            request.ConversationId,
            request.Request.Role,
            request.Request.Content);

        // Apply metadata
        if (request.Request.Content.Contains('?'))
        {
            message.MarkAsQuestion();
        }

        // Extract entities and store as semantic facts in background
        _ = Task.Run(async () =>
        {
            try
            {
                ExtractedEntity[] entities;
                string extractionMethod;

                // PHASE 1: Determine extraction strategy based on configuration
                if (_heuristicConfig.UseHeuristicFirst || _heuristicConfig.HeuristicOnly)
                {
                    // Try heuristic extraction first (includes narrative fallback if configured)
                    var heuristicEntities = HeuristicFactExtractor.Extract(
                        message.Content,
                        _heuristicConfig.UseNarrativeFallback,
                        _heuristicConfig.NarrativeImportanceScore,
                        _heuristicConfig.MaxNarrativeFragmentsPerMessage);

                    if (_heuristicConfig.HeuristicOnly)
                    {
                        // Use ONLY heuristics, never call LLM
                        entities = heuristicEntities.ToArray();
                        extractionMethod = "heuristic-only";
                    }
                    else if (heuristicEntities.Count >= _heuristicConfig.MinHeuristicFactsForAI)
                    {
                        // Heuristics found enough facts, skip LLM for cost optimization
                        entities = heuristicEntities.ToArray();
                        extractionMethod = "heuristic-sufficient";
                    }
                    else
                    {
                        // Heuristics insufficient, use LLM and merge results
                        var llmEntities = await _llm.ExtractEntitiesAsync(message.Content, cancellationToken);

                        // Merge: LLM entities + heuristic entities, deduplicate by Key+Value
                        var merged = llmEntities.ToList();
                        foreach (var he in heuristicEntities)
                        {
                            // Add heuristic entity if not already present (case-insensitive)
                            if (!merged.Any(e =>
                                e.Key.Equals(he.Key, StringComparison.OrdinalIgnoreCase) &&
                                e.Value.Equals(he.Value, StringComparison.OrdinalIgnoreCase)))
                            {
                                merged.Add(he);
                            }
                        }

                        entities = merged.ToArray();
                        extractionMethod = "heuristic-plus-llm";
                    }
                }
                else
                {
                    // Traditional: LLM-only extraction (original behavior)
                    entities = await _llm.ExtractEntitiesAsync(message.Content, cancellationToken);
                    extractionMethod = "llm-only";
                }

                // Log extraction method if enabled
                if (_heuristicConfig.LogExtractionMethod)
                {
                    _logger.LogInformation(
                        "Extracted {Count} entities using method: {Method} (message {MessageId})",
                        entities.Length,
                        extractionMethod,
                        message.Id);
                }

                message.SetExtractedEntities(entities);

                // Note: Semantic facts are now stored directly in MemoryOrchestrator.StoreAsync
                // This background task only logs extraction results for monitoring
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract entities for message {MessageId}", message.Id);
            }
        }, cancellationToken);

        // Store through orchestrator (includes importance scoring)
        await _orchestrator.StoreAsync(
            request.UserId,
            request.ConversationId,
            message,
            cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} stored with importance score {Score:F2}",
            message.Id,
            message.Metadata.ImportanceScore);

        // Return response
        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            ImportanceScore = message.Metadata.ImportanceScore
        };
    }
}
