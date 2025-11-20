using MediatR;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.DTOs;
using MemoryKit.Domain.Entities;

namespace MemoryKit.Application.UseCases.CreateConversation;

/// <summary>
/// Command to create a new conversation.
/// </summary>
public record CreateConversationCommand(
    string UserId,
    CreateConversationRequest Request) : IRequest<ConversationResponse>;

/// <summary>
/// Handler for CreateConversationCommand.
/// </summary>
public class CreateConversationHandler : IRequestHandler<CreateConversationCommand, ConversationResponse>
{
    private readonly ILogger<CreateConversationHandler> _logger;

    public CreateConversationHandler(ILogger<CreateConversationHandler> logger)
    {
        _logger = logger;
    }

    public Task<ConversationResponse> Handle(
        CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating conversation '{Title}' for user {UserId}",
            request.Request.Title,
            request.UserId);

        // Create conversation entity
        var conversation = Conversation.Create(
            request.UserId,
            request.Request.Title,
            request.Request.Description);

        // Add tags if provided
        if (request.Request.Tags?.Length > 0)
        {
            conversation.AddTags(request.Request.Tags);
        }

        _logger.LogInformation(
            "Conversation {ConversationId} created for user {UserId}",
            conversation.Id,
            request.UserId);

        // Return response
        var response = new ConversationResponse
        {
            Id = conversation.Id,
            UserId = conversation.UserId,
            Title = conversation.Title,
            MessageCount = conversation.MessageCount,
            LastActivityAt = conversation.LastActivityAt
        };

        return Task.FromResult(response);
    }
}
