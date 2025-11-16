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
    private readonly ILogger<QueryMemoryHandler> _logger;

    public QueryMemoryHandler(ILogger<QueryMemoryHandler> logger)
    {
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

        // TODO: Retrieve context from IMemoryOrchestrator
        // TODO: Classify query using PrefrontalController
        // TODO: Generate response using SemanticKernelService
        // TODO: Assemble sources

        throw new NotImplementedException("QueryMemoryHandler not yet implemented");
    }
}
