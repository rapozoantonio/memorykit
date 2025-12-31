using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.PostgreSQL.Services;

/// <summary>
/// PostgreSQL implementation of IWorkingMemoryService.
/// Simple wrapper around IWorkingMemoryRepository.
/// </summary>
public class PostgresWorkingMemoryService : IWorkingMemoryService
{
    private readonly IWorkingMemoryRepository _repository;
    private readonly ILogger<PostgresWorkingMemoryService> _logger;

    public PostgresWorkingMemoryService(
        IWorkingMemoryRepository repository,
        ILogger<PostgresWorkingMemoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding message to PostgreSQL working memory for user {UserId}", userId);
        await _repository.AddAsync(userId, conversationId, message, cancellationToken);
    }

    public async Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting recent messages from PostgreSQL for user {UserId}", userId);
        return await _repository.GetRecentAsync(userId, conversationId, count, cancellationToken);
    }

    public async Task ClearAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Clearing PostgreSQL working memory for conversation {ConversationId}", conversationId);
        await _repository.ClearAsync(userId, conversationId, cancellationToken);
    }

    public async Task RemoveAsync(
        string userId,
        string conversationId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing message {MessageId} from PostgreSQL working memory", messageId);
        await _repository.RemoveAsync(userId, conversationId, messageId, cancellationToken);
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting all PostgreSQL working memory for user {UserId}", userId);
        await _repository.DeleteUserDataAsync(userId, cancellationToken);
    }
}
