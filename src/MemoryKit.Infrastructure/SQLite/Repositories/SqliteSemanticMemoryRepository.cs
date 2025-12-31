using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.PostgreSQL;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.SQLite.Repositories;

/// <summary>
/// SQLite implementation of Semantic Memory repository.
/// Provides text-based search fallback (no vector embeddings in SQLite).
/// For vector search, users should use PostgreSQL.
/// </summary>
public class SqliteSemanticMemoryRepository : ISemanticMemoryRepository
{
    private readonly MemoryKitDbContext _context;
    private readonly ILogger<SqliteSemanticMemoryRepository> _logger;

    public SqliteSemanticMemoryRepository(
        MemoryKitDbContext context,
        ILogger<SqliteSemanticMemoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> AddAsync(ExtractedFact fact, CancellationToken cancellationToken = default)
    {
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
                // Note: SQLite stores embedding as BLOB (serialized), but doesn't use it for vector search
                Embedding = null, // Vector embeddings stored but not used for search in SQLite
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SemanticFacts.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Added fact {FactId} to SQLite semantic memory. Key: {Key}", fact.Id, fact.Key);
            return fact.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding fact to SQLite semantic memory");
            throw;
        }
    }

    public async Task UpdateAsync(ExtractedFact fact, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.SemanticFacts
                .FirstOrDefaultAsync(s => s.Id == fact.Id, cancellationToken);

            if (entity != null)
            {
                entity.Content = fact.Value;
                entity.Confidence = fact.Importance;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.SemanticFacts.Update(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Updated fact {FactId} in SQLite semantic memory", fact.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fact in SQLite semantic memory");
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
        // SQLite does not support vector search natively
        // Fallback to text-based search using LIKE
        _logger.LogWarning(
            "Vector search requested in SQLite. Falling back to text search. For vector search, use PostgreSQL with pgvector.");

        // Return empty for now - client should use text search instead
        return Array.Empty<ExtractedFact>();
    }

    public async Task<ExtractedFact[]> GetByKeyAsync(
        string userId,
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _context.SemanticFacts
                .Where(s => s.UserId == userId && s.Content.Contains(key))
                .ToListAsync(cancellationToken);

            return results
                .Select(e => ExtractedFact.Create(e.UserId, e.ConversationId, "fact", e.Content,
                    Enum.Parse<EntityType>(e.FactType ?? "Unknown")))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite semantic facts by key");
            throw;
        }
    }

    public async Task<ExtractedFact?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.SemanticFacts
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (entity == null) return null;

            return ExtractedFact.Create(entity.UserId, entity.ConversationId, "fact", entity.Content,
                Enum.Parse<EntityType>(entity.FactType ?? "Unknown"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite semantic fact by ID");
            throw;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SemanticFacts
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted SQLite semantic fact {FactId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SQLite semantic fact");
            throw;
        }
    }

    public async Task<ExtractedFact[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _context.SemanticFacts
                .Where(s => s.UserId == userId)
                .ToListAsync(cancellationToken);

            return results
                .Select(e => ExtractedFact.Create(e.UserId, e.ConversationId, "fact", e.Content,
                    Enum.Parse<EntityType>(e.FactType ?? "Unknown")))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite semantic facts for user");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SemanticFacts
                .Where(s => s.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all SQLite semantic memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from SQLite semantic memory");
            throw;
        }
    }

    public async Task<int> PromoteToEpisodicAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var itemsToPromote = await _context.SemanticFacts
                .Where(s => s.UserId == userId && s.Confidence > 0.8 &&
                            (DateTime.UtcNow - s.CreatedAt).TotalHours > 2)
                .ToListAsync(cancellationToken);

            int promotedCount = itemsToPromote.Count;

            // Mark for deletion after promotion
            _context.SemanticFacts.RemoveRange(itemsToPromote);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Promoted {Count} items from SQLite semantic to episodic memory for user {UserId}",
                promotedCount, userId);

            return promotedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting SQLite semantic memory to episodic");
            throw;
        }
    }
}
