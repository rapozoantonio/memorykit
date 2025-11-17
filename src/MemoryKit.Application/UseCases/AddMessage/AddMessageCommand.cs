using MediatR;
using MemoryKit.Application.DTOs;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Application.UseCases.AddMessage;

/// <summary>
/// Command to add a message to a conversation.
/// </summary>
public record AddMessageCommand(
    string UserId,
    string ConversationId,
    CreateMessageRequest Request) : IRequest<MessageResponse>;

/// <summary>
/// Handler for AddMessageCommand.
/// </summary>
public class AddMessageHandler : IRequestHandler<AddMessageCommand, MessageResponse>
{
    private readonly IMemoryOrchestrator _orchestrator;
    private readonly ILogger<AddMessageHandler> _logger;

    public AddMessageHandler(
        IMemoryOrchestrator orchestrator,
        ILogger<AddMessageHandler> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MessageResponse> Handle(
        AddMessageCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding message to conversation {ConversationId} for user {UserId}",
            request.ConversationId,
            request.UserId);

        // Create message entity
        var message = Message.Create(
            request.UserId,
            request.ConversationId,
            request.Request.Role,
            request.Request.Content);

        // Apply metadata
        if (request.Request.Content.Contains('?'))
            message.MarkAsQuestion();

        if (request.Request.Tags?.Length > 0)
            message.Metadata = message.Metadata with { Tags = request.Request.Tags };

        // Store through orchestrator (handles importance scoring and entity extraction)
        await _orchestrator.StoreAsync(
            request.UserId,
            request.ConversationId,
            message,
            cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} stored with importance score {Score:F3}",
            message.Id,
            message.Metadata.ImportanceScore);

        // Return response
        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            ImportanceScore = message.Metadata.ImportanceScore
        };
    }
}
