using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.PostgreSQL.Services;

/// <summary>
/// PostgreSQL implementation of IEpisodicMemoryService.
/// Wraps IEpisodicMemoryRepository for episodic event storage.
/// </summary>
public class PostgresEpisodicMemoryService : IEpisodicMemoryService
{
    private readonly IEpisodicMemoryRepository _repository;
    private readonly ISemanticKernelService _embeddingService;
    private readonly ILogger<PostgresEpisodicMemoryService> _logger;

    public PostgresEpisodicMemoryService(
        IEpisodicMemoryRepository repository,
        ISemanticKernelService embeddingService,
        ILogger<PostgresEpisodicMemoryService> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task ArchiveAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Archiving message to PostgreSQL episodic memory");

        await _repository.AddEventAsync(
            message.UserId,
            message.ConversationId,
            "message",
            message.Content,
            message.Timestamp,
            cancellationToken);
    }

    public async Task<Message[]> SearchAsync(
        string userId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching episodic memory in PostgreSQL for user {UserId}", userId);

        var events = await _repository.GetByUserAsync(userId, cancellationToken);

        // Convert events to messages (simplified mapping)
        return events
            .Where(e => e.EventType == "message" && e.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .Select(e => Message.Create(
                e.UserId,
                e.ConversationId,
                MessageRole.Assistant, // Default role
                e.Content))
            .ToArray();
    }

    public async Task<Message?> GetAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting episodic memory from PostgreSQL with ID {MessageId}", messageId);

        var evt = await _repository.GetEventByIdAsync(messageId, cancellationToken);
        
        if (evt == null || evt.EventType != "message")
            return null;

        return Message.Create(
            evt.UserId,
            evt.ConversationId,
            MessageRole.Assistant,
            evt.Content);
    }

    public async Task DeleteAsync(
        string userId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting episodic memory {MessageId} for user {UserId}", messageId, userId);
        await _repository.DeleteEventAsync(messageId, cancellationToken);
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting all PostgreSQL episodic memory for user {UserId}", userId);
        await _repository.DeleteUserDataAsync(userId, cancellationToken);
    }
}
