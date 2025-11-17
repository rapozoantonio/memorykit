using System.Diagnostics;
using MediatR;
using MemoryKit.Application.DTOs;
using MemoryKit.Domain.Interfaces;

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
    private readonly IMemoryOrchestrator _orchestrator;
    private readonly ILogger<QueryMemoryHandler> _logger;

    public QueryMemoryHandler(
        IMemoryOrchestrator orchestrator,
        ILogger<QueryMemoryHandler> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QueryMemoryResponse> Handle(
        QueryMemoryQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Querying memory for user {UserId}, conversation {ConversationId}, query: {Query}",
            request.UserId,
            request.ConversationId,
            request.Request.Question.Substring(0, Math.Min(50, request.Request.Question.Length)));

        var stopwatch = Stopwatch.StartNew();

        // Retrieve context from memory orchestrator
        var context = await _orchestrator.RetrieveContextAsync(
            request.UserId,
            request.ConversationId,
            request.Request.Question,
            cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "Context retrieved in {ElapsedMs}ms: {WorkingMemoryCount} recent, {FactCount} facts, {ArchivedCount} archived, {TokenCount} tokens",
            stopwatch.ElapsedMilliseconds,
            context.WorkingMemory.Length,
            context.Facts.Length,
            context.ArchivedMessages.Length,
            context.TotalTokens);

        // For MVP, generate a simple response based on context
        // In production, this would call SemanticKernelService to generate LLM response
        var answer = GenerateSimpleResponse(context, request.Request.Question);

        // Assemble sources for transparency
        var sources = new List<MemorySource>();

        // Add working memory sources
        foreach (var msg in context.WorkingMemory.Take(3))
        {
            sources.Add(new MemorySource
            {
                Type = "WorkingMemory",
                Content = msg.Content.Substring(0, Math.Min(100, msg.Content.Length)),
                Timestamp = msg.Timestamp,
                RelevanceScore = 0.9
            });
        }

        // Add semantic memory sources
        foreach (var fact in context.Facts.Take(3))
        {
            sources.Add(new MemorySource
            {
                Type = "SemanticMemory",
                Content = $"{fact.Key}: {fact.Value}",
                RelevanceScore = fact.Importance
            });
        }

        // Build response
        var response = new QueryMemoryResponse
        {
            Answer = answer,
            Sources = sources.ToArray()
        };

        // Add debug info if requested
        if (request.Request.IncludeDebugInfo)
        {
            response.DebugInfo = new DebugInfo
            {
                QueryPlan = new
                {
                    Type = context.QueryPlan.Type.ToString(),
                    LayersUsed = context.QueryPlan.LayersToUse.Select(l => l.ToString()).ToArray()
                },
                TokensUsed = context.TotalTokens,
                RetrievalTimeMs = stopwatch.ElapsedMilliseconds,
                ContextSummary = new
                {
                    WorkingMemoryCount = context.WorkingMemory.Length,
                    FactCount = context.Facts.Length,
                    ArchivedCount = context.ArchivedMessages.Length,
                    AppliedProcedure = context.AppliedProcedure?.Name
                }
            };
        }

        return response;
    }

    /// <summary>
    /// Generates a simple response for MVP.
    /// Production would use SemanticKernelService with actual LLM.
    /// </summary>
    private string GenerateSimpleResponse(MemoryContext context, string question)
    {
        var response = new System.Text.StringBuilder();

        response.AppendLine($"[MVP Response - Question: {question}]");
        response.AppendLine();

        if (context.AppliedProcedure != null)
        {
            response.AppendLine($"Applied Procedure: {context.AppliedProcedure.Name}");
            response.AppendLine($"Instruction: {context.AppliedProcedure.InstructionTemplate}");
            response.AppendLine();
        }

        response.AppendLine($"Retrieved context from {context.QueryPlan.LayersToUse.Count} memory layer(s):");
        response.AppendLine($"- Recent messages: {context.WorkingMemory.Length}");
        response.AppendLine($"- Facts: {context.Facts.Length}");
        response.AppendLine($"- Archived messages: {context.ArchivedMessages.Length}");
        response.AppendLine();

        if (context.Facts.Any())
        {
            response.AppendLine("Relevant Facts:");
            foreach (var fact in context.Facts.Take(5))
            {
                response.AppendLine($"  • {fact.Key}: {fact.Value}");
            }
        }

        return response.ToString();
    }
}
