using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.PostgreSQL;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.SQLite.Repositories;

/// <summary>
/// SQLite implementation of Working Memory repository.
/// Provides local persistence for working memory without requiring external database.
/// </summary>
public class SqliteWorkingMemoryRepository : IWorkingMemoryRepository
{
    private readonly MemoryKitDbContext _context;
    private readonly ILogger<SqliteWorkingMemoryRepository> _logger;

    public SqliteWorkingMemoryRepository(
        MemoryKitDbContext context,
        ILogger<SqliteWorkingMemoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
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

            _context.WorkingMemories.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Added message {MessageId} to SQLite working memory. UserId: {UserId}, ConvId: {ConversationId}",
                message.Id, userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to SQLite working memory. UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.WorkingMemories
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
            _logger.LogError(ex, "Error retrieving SQLite working memory. UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task RemoveAsync(
        string userId,
        string conversationId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.WorkingMemories
                .FirstOrDefaultAsync(w => w.Id == messageId && w.UserId == userId, cancellationToken);

            if (entity != null)
            {
                _context.WorkingMemories.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Removed message {MessageId} from SQLite working memory", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing message from SQLite working memory");
            throw;
        }
    }

    public async Task ClearAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.WorkingMemories
                .Where(w => w.UserId == userId && w.ConversationId == conversationId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Cleared SQLite working memory for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing SQLite working memory");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.WorkingMemories
                .Where(w => w.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all SQLite working memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from SQLite working memory");
            throw;
        }
    }

    public async Task<int> PromoteToSemanticAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var itemsToPromote = await _context.WorkingMemories
                .Where(w => w.UserId == userId && 
                            (w.Importance > 0.7 || (DateTime.UtcNow - w.CreatedAt).TotalMinutes > 15))
                .ToListAsync(cancellationToken);

            int promotedCount = itemsToPromote.Count;

            // Mark for deletion after promotion
            _context.WorkingMemories.RemoveRange(itemsToPromote);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Promoted {Count} items from SQLite working to semantic memory for user {UserId}",
                promotedCount, userId);

            return promotedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting SQLite working memory to semantic");
            throw;
        }
    }
}
