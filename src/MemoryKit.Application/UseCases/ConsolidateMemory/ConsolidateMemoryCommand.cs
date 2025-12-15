using MediatR;
using MemoryKit.Application.DTOs;

namespace MemoryKit.Application.UseCases.ConsolidateMemory;

/// <summary>
/// Command for triggering memory consolidation.
/// </summary>
public record ConsolidateMemoryCommand(
    string UserId,
    string ConversationId,
    bool Force = false,
    string? TargetLayer = null) : IRequest<ConsolidateMemoryResponse>;
