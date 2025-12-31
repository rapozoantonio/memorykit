using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace MemoryKit.Infrastructure.PostgreSQL.Repositories;

/// <summary>
/// PostgreSQL implementation of Working Memory repository.
/// </summary>
public class PostgresWorkingMemoryRepository : IWorkingMemoryRepository
{
    private readonly IDbContextFactory<MemoryKitDbContext> _contextFactory;
    private readonly ILogger<PostgresWorkingMemoryRepository> _logger;

    public PostgresWorkingMemoryRepository(
        IDbContextFactory<MemoryKitDbContext> contextFactory,
        ILogger<PostgresWorkingMemoryRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var entity = new WorkingMemoryEntity
            {
                Id = message.Id,
                UserId = userId,
                ConversationId = conversationId,
                Content = message.Content,
                Importance = message.Metadata.ImportanceScore,
                CreatedAt = message.Timestamp,
                ExpiresAt = DateTime.UtcNow.AddHours(1) // 1-hour TTL for working memory
            };

            context.WorkingMemories.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Added message {MessageId} to working memory. UserId: {UserId}, ConvId: {ConversationId}",
                message.Id, userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to working memory. UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var entities = await context.WorkingMemories
                .Where(w => w.UserId == userId && w.ConversationId == conversationId)
                .Where(w => w.ExpiresAt == null || w.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(w => w.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return entities
                .OrderBy(e => e.CreatedAt)
                .Select(e => Message.Create(e.UserId, e.ConversationId, MessageRole.User, e.Content))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving working memory. UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task RemoveAsync(
        string userId,
        string conversationId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var entity = await context.WorkingMemories
                .FirstOrDefaultAsync(w => w.Id == messageId && w.UserId == userId, cancellationToken);

            if (entity != null)
            {
                context.WorkingMemories.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Removed message {MessageId} from working memory", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing message from working memory");
            throw;
        }
    }

    public async Task ClearAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            await context.WorkingMemories
                .Where(w => w.UserId == userId && w.ConversationId == conversationId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Cleared working memory for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing working memory");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            await context.WorkingMemories
                .Where(w => w.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all working memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from working memory");
            throw;
        }
    }

    public async Task<int> PromoteToSemanticAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var itemsToPromote = await context.WorkingMemories
                .Where(w => w.UserId == userId &&
                            (w.Importance > 0.7 || (DateTime.UtcNow - w.CreatedAt).TotalMinutes > 15))
                .ToListAsync(cancellationToken);

            int promotedCount = itemsToPromote.Count;

            // Mark for deletion after promotion
            context.WorkingMemories.RemoveRange(itemsToPromote);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Promoted {Count} items from working to semantic memory for user {UserId}",
                promotedCount, userId);

            return promotedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting working memory to semantic");
            throw;
        }
    }
}

/// <summary>
/// PostgreSQL implementation of Semantic Memory repository.
/// </summary>
public class PostgresSemanticMemoryRepository : ISemanticMemoryRepository
{
    private readonly IDbContextFactory<MemoryKitDbContext> _contextFactory;
    private readonly ILogger<PostgresSemanticMemoryRepository> _logger;

    public PostgresSemanticMemoryRepository(
        IDbContextFactory<MemoryKitDbContext> contextFactory,
        ILogger<PostgresSemanticMemoryRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<string> AddAsync(ExtractedFact fact, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var entity = new SemanticFactEntity
            {
                Id = fact.Id,
                UserId = fact.UserId,
                ConversationId = fact.ConversationId,
                Content = fact.Value,
                FactType = fact.Type.ToString(),
                Confidence = fact.Importance,
                Embedding = fact.Embedding != null ? new Vector(fact.Embedding) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.SemanticFacts.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Added fact {FactId} to semantic memory. Key: {Key}", fact.Id, fact.Key);
            return fact.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding fact to semantic memory");
            throw;
        }
    }

    public async Task UpdateAsync(ExtractedFact fact, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var entity = await context.SemanticFacts
                .FirstOrDefaultAsync(s => s.Id == fact.Id, cancellationToken);

            if (entity != null)
            {
                entity.Content = fact.Value;
                entity.Confidence = fact.Importance;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.Embedding = fact.Embedding != null ? new Vector(fact.Embedding) : null;

                context.SemanticFacts.Update(entity);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Updated fact {FactId} in semantic memory", fact.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fact in semantic memory");
            throw;
        }
    }

    public async Task<ExtractedFact[]> SearchByEmbeddingAsync(
        string userId,
        float[] embedding,
        double similarityThreshold = 0.7,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            // For now, get all facts and sort in memory. TODO: Use pgvector distance operators when available
            var allFacts = await context.SemanticFacts
                .Where(s => s.UserId == userId && s.Embedding != null)
                .ToListAsync(cancellationToken);

            var results = allFacts
                .Select(s => new
                {
                    Fact = s,
                    Distance = CosineSimilarity(s.Embedding!.ToArray(), embedding)
                })
                .OrderByDescending(s => s.Distance)
                .Take(maxResults)
                .Select(s => s.Fact)
                .ToList();

            return results
                .Select(e => ExtractedFact.Create(
                    e.UserId,
                    e.ConversationId,
                    "fact",
                    e.Content,
                    Enum.Parse<EntityType>(e.FactType ?? "Unknown")))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching semantic memory by embedding");
            throw;
        }
    }

    public async Task<ExtractedFact[]> GetByKeyAsync(
        string userId,
        string key,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var results = await context.SemanticFacts
                .Where(s => s.UserId == userId && s.Content.Contains(key))
                .ToListAsync(cancellationToken);

            return results
                .Select(e => ExtractedFact.Create(e.UserId, e.ConversationId, "fact", e.Content,
                    Enum.Parse<EntityType>(e.FactType ?? "Unknown")))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving semantic facts by key");
            throw;
        }
    }

    public async Task<ExtractedFact?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var entity = await context.SemanticFacts
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (entity == null) return null;

            return ExtractedFact.Create(entity.UserId, entity.ConversationId, "fact", entity.Content,
                Enum.Parse<EntityType>(entity.FactType ?? "Unknown"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving semantic fact by ID");
            throw;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            await context.SemanticFacts
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted semantic fact {FactId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting semantic fact");
            throw;
        }
    }

    public async Task<ExtractedFact[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var results = await context.SemanticFacts
                .Where(s => s.UserId == userId)
                .ToListAsync(cancellationToken);

            return results
                .Select(e => ExtractedFact.Create(e.UserId, e.ConversationId, "fact", e.Content,
                    Enum.Parse<EntityType>(e.FactType ?? "Unknown")))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving semantic facts for user");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            await context.SemanticFacts
                .Where(s => s.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all semantic memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from semantic memory");
            throw;
        }
    }

    public async Task<int> PromoteToEpisodicAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            var itemsToPromote = await context.SemanticFacts
                .Where(s => s.UserId == userId && s.Confidence > 0.8 &&
                            (DateTime.UtcNow - s.CreatedAt).TotalHours > 2)
                .ToListAsync(cancellationToken);

            int promotedCount = itemsToPromote.Count;

            // Mark for deletion after promotion
            context.SemanticFacts.RemoveRange(itemsToPromote);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Promoted {Count} items from semantic to episodic memory for user {UserId}",
                promotedCount, userId);

            return promotedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting semantic memory to episodic");
            throw;
        }
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0.0;

        double dotProduct = 0.0;
        double magnitudeA = 0.0;
        double magnitudeB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0.0 || magnitudeB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}

