using MediatR;
using MemoryKit.Application.DTOs;

namespace MemoryKit.Application.UseCases.QueryMemory;

/// <summary>
/// Query to retrieve memory context and generate a response.
/// </summary>
public record QueryMemoryQuery(
    string UserId,
    string ConversationId,
    QueryMemoryRequest Request) : IRequest<QueryMemoryResponse>;

/// <summary>
/// Handler for QueryMemoryQuery.
/// </summary>
public class QueryMemoryHandler : IRequestHandler<QueryMemoryQuery, QueryMemoryResponse>
{
    private readonly Domain.Interfaces.IMemoryOrchestrator _orchestrator;
    private readonly Infrastructure.SemanticKernel.ISemanticKernelService _llm;
    private readonly ILogger<QueryMemoryHandler> _logger;

    public QueryMemoryHandler(
        Domain.Interfaces.IMemoryOrchestrator orchestrator,
        Infrastructure.SemanticKernel.ISemanticKernelService llm,
        ILogger<QueryMemoryHandler> logger)
    {
        _orchestrator = orchestrator;
        _llm = llm;
        _logger = logger;
    }

    public async Task<QueryMemoryResponse> Handle(
        QueryMemoryQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Querying memory for user {UserId}, conversation {ConversationId}",
            request.UserId,
            request.ConversationId);

        var startTime = DateTime.UtcNow;

        // Retrieve context from orchestrator
        var memoryContext = await _orchestrator.RetrieveContextAsync(
            request.UserId,
            request.ConversationId,
            request.Request.Question,
            cancellationToken);

        var retrievalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Generate answer using LLM with context
        var answer = await _llm.AnswerWithContextAsync(
            request.Request.Question,
            memoryContext,
            cancellationToken);

        var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Assemble sources
        var sources = new List<MemorySource>();

        if (memoryContext.WorkingMemory.Length > 0)
        {
            sources.Add(new MemorySource
            {
                Type = "WorkingMemory",
                Content = $"{memoryContext.WorkingMemory.Length} recent messages",
                RelevanceScore = 1.0
            });
        }

        if (memoryContext.Facts.Length > 0)
        {
            sources.Add(new MemorySource
            {
                Type = "SemanticMemory",
                Content = $"{memoryContext.Facts.Length} relevant facts",
                RelevanceScore = 0.9
            });
        }

        if (memoryContext.ArchivedMessages.Length > 0)
        {
            sources.Add(new MemorySource
            {
                Type = "EpisodicMemory",
                Content = $"{memoryContext.ArchivedMessages.Length} archived messages",
                RelevanceScore = 0.8
            });
        }

        if (memoryContext.AppliedProcedure != null)
        {
            sources.Add(new MemorySource
            {
                Type = "ProceduralMemory",
                Content = $"Pattern: {memoryContext.AppliedProcedure.Name}",
                RelevanceScore = 1.0
            });
        }

        _logger.LogInformation(
            "Query completed: {TotalTime}ms (retrieval: {RetrievalTime}ms), {Tokens} tokens",
            totalTime,
            retrievalTime,
            memoryContext.TotalTokens);

        // Prepare debug info
        DebugInfo? debugInfo = null;
        if (request.Request.IncludeDebugInfo)
        {
            debugInfo = new DebugInfo
            {
                QueryPlanType = memoryContext.QueryPlan.Type.ToString(),
                LayersUsed = memoryContext.QueryPlan.LayersToUse.Select(l => l.ToString()).ToArray(),
                TokensUsed = memoryContext.TotalTokens,
                RetrievalTimeMs = (long)retrievalTime
            };
        }

        return new QueryMemoryResponse
        {
            Answer = answer,
            Sources = sources.ToArray(),
            DebugInfo = debugInfo
        };
    }
}
