using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.PostgreSQL.Services;

/// <summary>
/// PostgreSQL implementation of IScratchpadService.
/// Wraps ISemanticMemoryRepository and adds embedding generation.
/// </summary>
public class PostgresScratchpadService : IScratchpadService
{
    private readonly ISemanticMemoryRepository _repository;
    private readonly ISemanticKernelService _embeddingService;
    private readonly ILogger<PostgresScratchpadService> _logger;

    public PostgresScratchpadService(
        ISemanticMemoryRepository repository,
        ISemanticKernelService embeddingService,
        ILogger<PostgresScratchpadService> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task StoreFactsAsync(
        string userId,
        string conversationId,
        ExtractedFact[] facts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Storing {Count} facts in PostgreSQL for user {UserId}", facts.Length, userId);

        foreach (var fact in facts)
        {
            // Generate embedding if not present
            if (fact.Embedding == null || fact.Embedding.Length == 0)
            {
                var embedding = await _embeddingService.GetEmbeddingAsync(fact.Value, cancellationToken);
                fact.SetEmbedding(embedding);
            }

            await _repository.AddAsync(fact, cancellationToken);
        }
    }

    public async Task<ExtractedFact[]> SearchFactsAsync(
        string userId,
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching facts in PostgreSQL for user {UserId} with query: {Query}", userId, query);

        // Generate embedding for the query
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);

        // Search using vector similarity
        return await _repository.SearchByEmbeddingAsync(userId, queryEmbedding, 0.7, maxResults, cancellationToken);
    }

    public async Task RecordAccessAsync(
        string factId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Recording access for fact {FactId}", factId);

        var fact = await _repository.GetByIdAsync(factId, cancellationToken);
        if (fact != null)
        {
            fact.RecordAccess();
            await _repository.UpdateAsync(fact, cancellationToken);
        }
    }

    public async Task PruneAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pruning old facts for user {UserId}", userId);

        var allFacts = await _repository.GetByUserAsync(userId, cancellationToken);
        var ttl = TimeSpan.FromDays(30); // Default TTL
        var minAccessCount = 3;
        var toDelete = allFacts.Where(f => f.ShouldEvict(ttl, minAccessCount)).ToArray();

        foreach (var fact in toDelete)
        {
            await _repository.DeleteAsync(fact.Id, cancellationToken);
        }

        _logger.LogInformation("Pruned {Count} facts for user {UserId}", toDelete.Length, userId);
    }

    public async Task DeleteFactAsync(
        string userId,
        string factId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting fact {FactId} for user {UserId}", factId, userId);
        await _repository.DeleteAsync(factId, cancellationToken);
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting all PostgreSQL semantic memory for user {UserId}", userId);
        await _repository.DeleteUserDataAsync(userId, cancellationToken);
    }
}
