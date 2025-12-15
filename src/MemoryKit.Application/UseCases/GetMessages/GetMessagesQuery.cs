using MediatR;
using MemoryKit.Application.DTOs;

namespace MemoryKit.Application.UseCases.GetMessages;

/// <summary>
/// Query for retrieving messages from a conversation.
/// </summary>
public record GetMessagesQuery(
    string UserId,
    string ConversationId,
    int? Limit = null,
    DateTime? Before = null,
    DateTime? After = null,
    string? Layer = null) : IRequest<GetMessagesResponse>;
