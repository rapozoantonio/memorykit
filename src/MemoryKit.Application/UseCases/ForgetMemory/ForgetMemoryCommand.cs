using MediatR;

namespace MemoryKit.Application.UseCases.ForgetMemory;

/// <summary>
/// Command for forgetting (deleting) a specific message from memory.
/// </summary>
public record ForgetMemoryCommand(
    string UserId,
    string ConversationId,
    string MessageId) : IRequest<bool>;
