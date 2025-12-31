using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.PostgreSQL.Services;

/// <summary>
/// PostgreSQL implementation of IProceduralMemoryService.
/// Wraps IProceduralMemoryRepository for procedural pattern storage.
/// </summary>
public class PostgresProceduralMemoryService : IProceduralMemoryService
{
    private readonly IProceduralMemoryRepository _repository;
    private readonly ILogger<PostgresProceduralMemoryService> _logger;

    public PostgresProceduralMemoryService(
        IProceduralMemoryRepository repository,
        ILogger<PostgresProceduralMemoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Matching patterns in PostgreSQL for user {UserId}", userId);

        var patterns = await _repository.GetByUserAsync(userId, cancellationToken);
        
        // Simple pattern matching (can be enhanced with semantic similarity)
        var match = patterns
            .Where(p => query.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
                       query.Contains(p.Description, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.UsageCount)
            .FirstOrDefault();

        return match;
    }

    public async Task StorePatternAsync(
        ProceduralPattern pattern,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Storing pattern in PostgreSQL for user {UserId}", pattern.UserId);
        
        var existing = await _repository.GetByIdAsync(pattern.Id, cancellationToken);
        
        if (existing == null)
        {
            await _repository.AddPatternAsync(
                pattern.UserId,
                pattern.Name,
                pattern.Name, // Using Name as pattern condition
                pattern.Description,
                cancellationToken);
        }
        else
        {
            await _repository.UpdatePatternAsync(pattern, cancellationToken);
        }
    }

    public async Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detecting patterns from message in PostgreSQL");

        // Simple pattern detection: create a pattern from the message
        await _repository.AddPatternAsync(
            userId,
            "message-pattern",
            message.Content.Substring(0, Math.Min(50, message.Content.Length)),
            message.Content,
            cancellationToken);
    }

    public async Task<ProceduralPattern[]> GetUserPatternsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all patterns for user {UserId} from PostgreSQL", userId);
        
        var patterns = await _repository.GetByUserAsync(userId, cancellationToken);
        return patterns.ToArray();
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting all PostgreSQL procedural memory for user {UserId}", userId);
        await _repository.DeleteUserDataAsync(userId, cancellationToken);
    }
}
