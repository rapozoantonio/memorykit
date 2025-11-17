using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using MemoryKit.Infrastructure.Azure;
using MemoryKit.Infrastructure.Cognitive;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Application.Services;

/// <summary>
/// Central coordinator for all memory operations.
/// Implements the orchestration logic from the TRD Section 6.1.
/// </summary>
public class MemoryOrchestrator : IMemoryOrchestrator
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly IScratchpadService _scratchpad;
    private readonly IEpisodicMemoryService _episodicMemory;
    private readonly IProceduralMemoryService _proceduralMemory;
    private readonly IPrefrontalController _prefrontal;
    private readonly IAmygdalaImportanceEngine _amygdala;
    private readonly ILogger<MemoryOrchestrator> _logger;

    public MemoryOrchestrator(
        IWorkingMemoryService workingMemory,
        IScratchpadService scratchpad,
        IEpisodicMemoryService episodicMemory,
        IProceduralMemoryService proceduralMemory,
        IPrefrontalController prefrontal,
        IAmygdalaImportanceEngine amygdala,
        ILogger<MemoryOrchestrator> logger)
    {
        _workingMemory = workingMemory;
        _scratchpad = scratchpad;
        _episodicMemory = episodicMemory;
        _proceduralMemory = proceduralMemory;
        _prefrontal = prefrontal;
        _amygdala = amygdala;
        _logger = logger;
    }

    public async Task<MemoryContext> RetrieveContextAsync(
        string userId,
        string conversationId,
        string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving context for user {UserId}, conversation {ConversationId}",
            userId,
            conversationId);

        // Step 1: Build query plan using Prefrontal Controller
        var state = new ConversationState
        {
            UserId = userId,
            ConversationId = conversationId,
            TurnCount = 0, // TODO: Track actual turn count
            LastQueryTime = DateTime.UtcNow
        };

        var plan = await _prefrontal.BuildQueryPlanAsync(query, state, cancellationToken);

        _logger.LogInformation(
            "Query plan: Type={Type}, Layers={Layers}",
            plan.Type,
            string.Join(", ", plan.LayersToUse));

        // Step 2: Retrieve from layers in parallel
        var retrievalTasks = new List<Task>();
        Message[] workingMemoryItems = Array.Empty<Message>();
        ExtractedFact[] facts = Array.Empty<ExtractedFact>();
        Message[] archivedMessages = Array.Empty<Message>();
        ProceduralPattern? matchedPattern = null;

        if (plan.LayersToUse.Contains(MemoryLayer.WorkingMemory))
        {
            retrievalTasks.Add(Task.Run(async () =>
            {
                workingMemoryItems = await _workingMemory.GetRecentAsync(
                    userId,
                    conversationId,
                    count: 10,
                    cancellationToken);

                _logger.LogDebug(
                    "Retrieved {Count} items from working memory",
                    workingMemoryItems.Length);
            }, cancellationToken));
        }

        if (plan.LayersToUse.Contains(MemoryLayer.SemanticMemory))
        {
            retrievalTasks.Add(Task.Run(async () =>
            {
                facts = await _scratchpad.SearchFactsAsync(
                    userId,
                    query,
                    maxResults: 20,
                    cancellationToken);

                _logger.LogDebug(
                    "Retrieved {Count} facts from semantic memory",
                    facts.Length);
            }, cancellationToken));
        }

        if (plan.LayersToUse.Contains(MemoryLayer.EpisodicMemory))
        {
            retrievalTasks.Add(Task.Run(async () =>
            {
                archivedMessages = await _episodicMemory.SearchAsync(
                    userId,
                    query,
                    maxResults: 5,
                    cancellationToken);

                _logger.LogDebug(
                    "Retrieved {Count} messages from episodic memory",
                    archivedMessages.Length);
            }, cancellationToken));
        }

        if (plan.LayersToUse.Contains(MemoryLayer.ProceduralMemory))
        {
            retrievalTasks.Add(Task.Run(async () =>
            {
                matchedPattern = await _proceduralMemory.MatchPatternAsync(
                    userId,
                    query,
                    cancellationToken);

                if (matchedPattern != null)
                {
                    _logger.LogInformation(
                        "Matched procedural pattern: {PatternName}",
                        matchedPattern.Name);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(retrievalTasks);

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
            "Context assembled: {TotalTokens} tokens from {LayerCount} layers",
            context.TotalTokens,
            plan.LayersToUse.Count);

        return context;
    }

    public async Task StoreAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Storing message {MessageId} for user {UserId}",
            message.Id,
            userId);

        // Step 1: Calculate importance using Amygdala
        var importance = await _amygdala.CalculateImportanceAsync(
            message,
            cancellationToken);

        message.MarkAsImportant(importance.FinalScore);

        _logger.LogDebug(
            "Message importance score: {Score}",
            importance.FinalScore);

        // Step 2: Store in all layers (parallel where safe)
        var storageTasks = new[]
        {
            // Layer 1: Archive everything
            _episodicMemory.ArchiveAsync(message, cancellationToken),

            // Layer 3: Update working memory
            _workingMemory.AddAsync(userId, conversationId, message, cancellationToken)
        };

        await Task.WhenAll(storageTasks);

        // Step 3: Background processing (fire-and-forget with error handling)
        _ = Task.Run(async () =>
        {
            try
            {
                // Detect and store procedural patterns
                await _proceduralMemory.DetectAndStorePatternAsync(
                    userId,
                    message,
                    cancellationToken);

                _logger.LogDebug(
                    "Background pattern detection completed for message {MessageId}",
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background processing failed for message {MessageId}",
                    message.Id);
            }
        }, cancellationToken);
    }

    public Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default)
    {
        return _prefrontal.BuildQueryPlanAsync(query, state, cancellationToken);
    }

    /// <summary>
    /// Calculates estimated token count for the assembled context.
    /// Simple heuristic: ~4 characters per token.
    /// </summary>
    private int CalculateTokenCount(
        Message[] workingMemory,
        ExtractedFact[] facts,
        Message[] archivedMessages,
        ProceduralPattern? pattern)
    {
        var totalChars = 0;

        // Working memory messages
        totalChars += workingMemory.Sum(m => m.Content.Length);

        // Facts (key + value)
        totalChars += facts.Sum(f => f.Key.Length + f.Value.Length);

        // Archived messages
        totalChars += archivedMessages.Sum(m => m.Content.Length);

        // Procedural pattern instruction
        if (pattern != null)
        {
            totalChars += pattern.InstructionTemplate.Length;
        }

        // Convert to approximate tokens (4 chars ≈ 1 token)
        return totalChars / 4;
    }
}
