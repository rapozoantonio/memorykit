using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Application.UseCases.GetContext;

/// <summary>
/// Query to get memory context without generating a response.
/// </summary>
public record GetContextQuery(
    string UserId,
    string ConversationId,
    string Query) : IRequest<GetContextResponse>;

/// <summary>
/// Response containing memory context.
/// </summary>
public record GetContextResponse
{
    /// <summary>
    /// Gets the formatted context as a string.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Gets the total tokens in the context.
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Gets the retrieval latency in milliseconds.
    /// </summary>
    public long RetrievalLatencyMs { get; init; }
}

/// <summary>
/// Handler for GetContextQuery.
/// </summary>
public class GetContextHandler : IRequestHandler<GetContextQuery, GetContextResponse>
{
    private readonly Domain.Interfaces.IMemoryOrchestrator _orchestrator;
    private readonly ILogger<GetContextHandler> _logger;

    public GetContextHandler(
        Domain.Interfaces.IMemoryOrchestrator orchestrator,
        ILogger<GetContextHandler> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<GetContextResponse> Handle(
        GetContextQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting context for user {UserId}, conversation {ConversationId}",
            request.UserId,
            request.ConversationId);

        var startTime = DateTime.UtcNow;

        // Retrieve context from orchestrator
        var memoryContext = await _orchestrator.RetrieveContextAsync(
            request.UserId,
            request.ConversationId,
            request.Query,
            cancellationToken);

        var latency = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Format context as string
        var formattedContext = memoryContext.ToPromptContext();

        _logger.LogInformation(
            "Retrieved context: {Tokens} tokens in {Latency}ms",
            memoryContext.TotalTokens,
            latency);

        return new GetContextResponse
        {
            Context = formattedContext,
            TotalTokens = memoryContext.TotalTokens,
            RetrievalLatencyMs = (long)latency
        };
    }
}
