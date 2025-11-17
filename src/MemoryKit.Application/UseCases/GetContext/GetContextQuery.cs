using System.Diagnostics;
using MediatR;
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
    private readonly IMemoryOrchestrator _orchestrator;
    private readonly ILogger<GetContextHandler> _logger;

    public GetContextHandler(
        IMemoryOrchestrator orchestrator,
        ILogger<GetContextHandler> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetContextResponse> Handle(
        GetContextQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting context for user {UserId}, conversation {ConversationId}",
            request.UserId,
            request.ConversationId);

        var stopwatch = Stopwatch.StartNew();

        // Retrieve context from orchestrator
        var context = await _orchestrator.RetrieveContextAsync(
            request.UserId,
            request.ConversationId,
            request.Query,
            cancellationToken);

        stopwatch.Stop();

        // Format context as string
        var formattedContext = context.ToPromptContext();

        _logger.LogInformation(
            "Context retrieved in {ElapsedMs}ms: {TokenCount} tokens",
            stopwatch.ElapsedMilliseconds,
            context.TotalTokens);

        return new GetContextResponse
        {
            Context = formattedContext,
            TotalTokens = context.TotalTokens,
            RetrievalLatencyMs = stopwatch.ElapsedMilliseconds
        };
    }
}
