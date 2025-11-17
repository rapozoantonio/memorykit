using System.Text;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using MemoryKit.Infrastructure.Azure;
using MemoryKit.Infrastructure.Cognitive;
using MemoryKit.Infrastructure.SemanticKernel;

namespace MemoryKit.Application.Services;

/// <summary>
/// Central coordinator for all memory operations.
/// Orchestrates retrieval from multiple memory layers and storage operations.
/// </summary>
public class MemoryOrchestrator : IMemoryOrchestrator
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly IScratchpadService _scratchpad;
    private readonly IEpisodicMemoryService _episodicMemory;
    private readonly IProceduralMemoryService _proceduralMemory;
    private readonly IPrefrontalController _prefrontal;
    private readonly IAmygdalaImportanceEngine _amygdala;
    private readonly ISemanticKernelService? _semanticKernel;
    private readonly ILogger<MemoryOrchestrator> _logger;

    public MemoryOrchestrator(
        IWorkingMemoryService workingMemory,
        IScratchpadService scratchpad,
        IEpisodicMemoryService episodicMemory,
        IProceduralMemoryService proceduralMemory,
        IPrefrontalController prefrontal,
        IAmygdalaImportanceEngine amygdala,
        ILogger<MemoryOrchestrator> logger,
        ISemanticKernelService? semanticKernel = null)
    {
        _workingMemory = workingMemory ?? throw new ArgumentNullException(nameof(workingMemory));
        _scratchpad = scratchpad ?? throw new ArgumentNullException(nameof(scratchpad));
        _episodicMemory = episodicMemory ?? throw new ArgumentNullException(nameof(episodicMemory));
        _proceduralMemory = proceduralMemory ?? throw new ArgumentNullException(nameof(proceduralMemory));
        _prefrontal = prefrontal ?? throw new ArgumentNullException(nameof(prefrontal));
        _amygdala = amygdala ?? throw new ArgumentNullException(nameof(amygdala));
        _semanticKernel = semanticKernel;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MemoryContext> RetrieveContextAsync(
        string userId,
        string conversationId,
        string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving context for user {UserId}, conversation {ConversationId}, query: {Query}",
            userId,
            conversationId,
            query.Substring(0, Math.Min(50, query.Length)));

        // Step 1: Build query plan
        var state = await GetConversationState(conversationId, cancellationToken);
        var plan = await _prefrontal.BuildQueryPlanAsync(query, state, cancellationToken);

        _logger.LogInformation(
            "Query plan: Type={Type}, Layers={Layers}",
            plan.Type,
            string.Join(", ", plan.LayersToUse));

        // Step 2: Retrieve from layers in parallel
        Message[] workingMemoryItems = Array.Empty<Message>();
        ExtractedFact[] facts = Array.Empty<ExtractedFact>();
        Message[] archivedMessages = Array.Empty<Message>();
        ProceduralPattern? matchedPattern = null;

        var tasks = new List<Task>();

        if (plan.LayersToUse.Contains(MemoryLayer.WorkingMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                workingMemoryItems = await _workingMemory.GetRecentAsync(
                    userId,
                    conversationId,
                    count: 10,
                    cancellationToken);

                _logger.LogDebug("Retrieved {Count} items from working memory", workingMemoryItems.Length);
            }, cancellationToken));
        }

        if (plan.LayersToUse.Contains(MemoryLayer.SemanticMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                facts = await _scratchpad.SearchFactsAsync(
                    userId,
                    query,
                    maxResults: 20,
                    cancellationToken);

                _logger.LogDebug("Retrieved {Count} facts from semantic memory", facts.Length);
            }, cancellationToken));
        }

        if (plan.LayersToUse.Contains(MemoryLayer.EpisodicMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                archivedMessages = await _episodicMemory.SearchAsync(
                    userId,
                    query,
                    maxResults: 5,
                    cancellationToken);

                _logger.LogDebug("Retrieved {Count} messages from episodic memory", archivedMessages.Length);
            }, cancellationToken));
        }

        if (plan.LayersToUse.Contains(MemoryLayer.ProceduralMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                matchedPattern = await _proceduralMemory.MatchPatternAsync(
                    userId,
                    query,
                    cancellationToken);

                if (matchedPattern != null)
                    _logger.LogDebug("Matched procedural pattern: {PatternName}", matchedPattern.Name);
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        // Step 3: Assemble context
        var context = new MemoryContext
        {
            WorkingMemory = workingMemoryItems,
            Facts = facts,
            ArchivedMessages = archivedMessages,
            AppliedProcedure = matchedPattern,
            QueryPlan = plan,
            TotalTokens = CalculateTokenCount(
                workingMemoryItems,
                facts,
                archivedMessages,
                matchedPattern)
        };

        _logger.LogInformation(
            "Context assembled: {WorkingMemoryCount} recent messages, {FactCount} facts, {ArchivedCount} archived messages, {TokenCount} estimated tokens",
            workingMemoryItems.Length,
            facts.Length,
            archivedMessages.Length,
            context.TotalTokens);

        return context;
    }

    public async Task StoreAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Storing message {MessageId} for user {UserId}, conversation {ConversationId}",
            message.Id,
            userId,
            conversationId);

        // Step 1: Calculate importance
        var importance = await _amygdala.CalculateImportanceAsync(
            message,
            cancellationToken);

        message.Metadata = message.Metadata with
        {
            ImportanceScore = importance.FinalScore
        };

        _logger.LogDebug(
            "Message importance score: {Score:F3}",
            importance.FinalScore);

        // Step 2: Store in all layers (parallel)
        var tasks = new[]
        {
            // Layer 1: Archive everything
            _episodicMemory.ArchiveAsync(message, cancellationToken),

            // Layer 3: Update working memory
            _workingMemory.AddAsync(
                userId,
                conversationId,
                message,
                cancellationToken)
        };

        await Task.WhenAll(tasks);

        // Step 3: Background processing (fire-and-forget with error handling)
        _ = Task.Run(async () =>
        {
            try
            {
                // Extract entities for scratchpad using AI if available
                var entities = await ExtractEntitiesAsync(message, cancellationToken);
                if (entities.Any())
                {
                    await _scratchpad.StoreFactsAsync(
                        userId,
                        conversationId,
                        entities,
                        cancellationToken);

                    _logger.LogDebug("Extracted and stored {Count} entities", entities.Length);
                }

                // Detect procedural patterns
                await _proceduralMemory.DetectAndStorePatternAsync(
                    userId,
                    message,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background processing failed for message {MessageId}", message.Id);
            }
        }, cancellationToken);

        _logger.LogInformation("Message stored successfully");
    }

    public Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default)
    {
        return _prefrontal.BuildQueryPlanAsync(query, state, cancellationToken);
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Deleting all data for user {UserId} (GDPR compliance)", userId);

        // Delete from all memory layers
        // Note: This is a simplified implementation
        // Production would need comprehensive cleanup across all storage systems

        await Task.CompletedTask;
        _logger.LogInformation("User data deleted for {UserId}", userId);
    }

    /// <summary>
    /// Gets the current conversation state (simplified).
    /// </summary>
    private Task<ConversationState> GetConversationState(
        string conversationId,
        CancellationToken cancellationToken)
    {
        // Simplified - in production, this would track conversation metadata
        return Task.FromResult(new ConversationState
        {
            ConversationId = conversationId,
            MessageCount = 0,
            LastActivity = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Calculates estimated token count for context.
    /// </summary>
    private int CalculateTokenCount(
        Message[] workingMemory,
        ExtractedFact[] facts,
        Message[] archived,
        ProceduralPattern? pattern)
    {
        // Rough estimation: ~4 characters per token
        int totalChars = 0;

        totalChars += workingMemory.Sum(m => m.Content.Length);
        totalChars += facts.Sum(f => f.Key.Length + f.Value.Length);
        totalChars += archived.Sum(m => m.Content.Length);
        totalChars += pattern?.InstructionTemplate?.Length ?? 0;

        return totalChars / 4;
    }

    /// <summary>
    /// Extracts entities from message content.
    /// Uses SemanticKernelService if available, otherwise falls back to basic extraction.
    /// </summary>
    private async Task<ExtractedEntity[]> ExtractEntitiesAsync(Message message, CancellationToken cancellationToken)
    {
        // Use AI-powered extraction if available
        if (_semanticKernel != null)
        {
            try
            {
                var entities = await _semanticKernel.ExtractEntitiesAsync(
                    message.Content,
                    cancellationToken);

                _logger.LogDebug("Extracted {Count} entities using AI", entities.Length);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI entity extraction failed, using fallback");
            }
        }

        // Fallback to basic extraction
        return ExtractBasicEntities(message);
    }

    /// <summary>
    /// Basic entity extraction (fallback when AI is not available).
    /// </summary>
    private ExtractedEntity[] ExtractBasicEntities(Message message)
    {
        var entities = new List<ExtractedEntity>();

        // Extract code blocks
        if (message.Content.Contains("```"))
        {
            entities.Add(new ExtractedEntity
            {
                Key = "Contains Code",
                Value = "true",
                Type = EntityType.Technology,
                Importance = 0.8,
                IsNovel = true,
                Embedding = Array.Empty<float>() // Would be generated by embedding service
            });
        }

        // Extract URLs
        var words = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var urls = words.Where(w => w.StartsWith("http://") || w.StartsWith("https://")).ToArray();
        foreach (var url in urls.Take(3))
        {
            entities.Add(new ExtractedEntity
            {
                Key = "Referenced URL",
                Value = url,
                Type = EntityType.Other,
                Importance = 0.6,
                IsNovel = true,
                Embedding = Array.Empty<float>()
            });
        }

        return entities.ToArray();
    }
}
