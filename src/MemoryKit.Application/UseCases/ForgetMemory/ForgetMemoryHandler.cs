using MediatR;
using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Application.UseCases.ForgetMemory;

/// <summary>
/// Handler for forgetting (deleting) a specific message from all memory layers.
/// </summary>
public class ForgetMemoryHandler : IRequestHandler<ForgetMemoryCommand, bool>
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly IEpisodicMemoryService _episodicMemory;
    private readonly IScratchpadService _scratchpad;
    private readonly ILogger<ForgetMemoryHandler> _logger;

    public ForgetMemoryHandler(
        IWorkingMemoryService workingMemory,
        IEpisodicMemoryService episodicMemory,
        IScratchpadService scratchpad,
        ILogger<ForgetMemoryHandler> logger)
    {
        _workingMemory = workingMemory;
        _episodicMemory = episodicMemory;
        _scratchpad = scratchpad;
        _logger = logger;
    }

    public async Task<bool> Handle(ForgetMemoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Forgetting message {MessageId} from conversation {ConversationId}",
            request.MessageId, request.ConversationId);

        try
        {
            // Remove from working memory
            await _workingMemory.RemoveAsync(
                request.UserId,
                request.ConversationId,
                request.MessageId,
                cancellationToken);

            // Remove from episodic memory
            await _episodicMemory.DeleteAsync(
                request.UserId,
                request.MessageId,
                cancellationToken);

            // Note: Scratchpad stores facts, not messages directly
            // Facts derived from this message would remain but that's acceptable
            // In a future version, we could track message-to-fact relationships

            _logger.LogInformation("Successfully forgot message {MessageId}", request.MessageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forgetting message {MessageId}", request.MessageId);
            return false;
        }
    }
}
