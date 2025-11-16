using MediatR;

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
    private readonly ILogger<GetContextHandler> _logger;

    public GetContextHandler(ILogger<GetContextHandler> logger)
    {
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

        // TODO: Retrieve context from IMemoryOrchestrator
        // TODO: Format context
        // TODO: Calculate tokens

        throw new NotImplementedException("GetContextHandler not yet implemented");
    }
}
