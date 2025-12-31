using MediatR;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.DTOs;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.Enums;

namespace MemoryKit.Application.UseCases.GetMessages;

/// <summary>
/// Handler for retrieving messages from a conversation.
/// </summary>
public class GetMessagesHandler : IRequestHandler<GetMessagesQuery, GetMessagesResponse>
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly IEpisodicMemoryService _episodicMemory;
    private readonly ILogger<GetMessagesHandler> _logger;

    public GetMessagesHandler(
        IWorkingMemoryService workingMemory,
        IEpisodicMemoryService episodicMemory,
        ILogger<GetMessagesHandler> logger)
    {
        _workingMemory = workingMemory;
        _episodicMemory = episodicMemory;
        _logger = logger;
    }

    public async Task<GetMessagesResponse> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving messages for conversation {ConversationId}, limit: {Limit}, layer: {Layer}",
            request.ConversationId, request.Limit, request.Layer);

        var messages = new List<MessageResponse>();

        // Determine which layers to query
        var queryWorking = string.IsNullOrEmpty(request.Layer) || request.Layer.Equals("working", StringComparison.OrdinalIgnoreCase);
        var queryEpisodic = string.IsNullOrEmpty(request.Layer) || request.Layer.Equals("episodic", StringComparison.OrdinalIgnoreCase);

        // Get messages from working memory
        if (queryWorking)
        {
            var workingMessages = await _workingMemory.GetRecentAsync(
                request.UserId,
                request.ConversationId,
                request.Limit ?? 50,
                cancellationToken);

            messages.AddRange(workingMessages.Select(m => new MessageResponse
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                ImportanceScore = m.Metadata.ImportanceScore
            }));
        }

        // Get messages from episodic memory if needed
        if (queryEpisodic && (request.Limit == null || messages.Count < request.Limit.Value))
        {
            var episodicMessages = await _episodicMemory.SearchAsync(
                request.UserId,
                request.ConversationId,  // Using conversationId as query to get all messages
                request.Limit ?? 50,
                cancellationToken);

            messages.AddRange(episodicMessages.Select(m => new MessageResponse
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                ImportanceScore = m.Metadata.ImportanceScore
            }));
        }

        // Apply time filters
        if (request.Before.HasValue)
        {
            messages = messages.Where(m => m.Timestamp < request.Before.Value).ToList();
        }

        if (request.After.HasValue)
        {
            messages = messages.Where(m => m.Timestamp > request.After.Value).ToList();
        }

        // Sort by timestamp and apply limit
        messages = messages
            .OrderByDescending(m => m.Timestamp)
            .Take(request.Limit ?? 50)
            .ToList();

        _logger.LogInformation("Retrieved {Count} messages", messages.Count);

        return new GetMessagesResponse
        {
            ConversationId = request.ConversationId,
            Messages = messages.ToArray(),
            Total = messages.Count,
            HasMore = false // For v0.1, simplified pagination
        };
    }
}
