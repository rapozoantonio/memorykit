using MemoryKit.Infrastructure.PostgreSQL;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.SQLite.Repositories;

/// <summary>
/// SQLite implementation of Episodic Memory repository.
/// Stores events and temporal information in local SQLite database.
/// </summary>
public class SqliteEpisodicMemoryRepository : IEpisodicMemoryRepository
{
    private readonly MemoryKitDbContext _context;
    private readonly ILogger<SqliteEpisodicMemoryRepository> _logger;

    public SqliteEpisodicMemoryRepository(
        MemoryKitDbContext context,
        ILogger<SqliteEpisodicMemoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> AddEventAsync(
        string userId,
        string conversationId,
        string eventType,
        string content,
        DateTime occurredAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new EpisodicEventEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                ConversationId = conversationId,
                EventType = eventType,
                Content = content,
                OccurredAt = occurredAt,
                DecayFactor = 1.0,
                CreatedAt = DateTime.UtcNow
            };

            _context.EpisodicEvents.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Added event {EventId} to SQLite episodic memory. Type: {EventType}, UserId: {UserId}",
                entity.Id, eventType, userId);

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding event to SQLite episodic memory");
            throw;
        }
    }

    public async Task<EpisodicEvent[]> GetEventsByTimeRangeAsync(
        string userId,
        string conversationId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.EpisodicEvents
                .Where(e => e.UserId == userId &&
                            e.ConversationId == conversationId &&
                            e.OccurredAt >= startTime &&
                            e.OccurredAt <= endTime)
                .OrderBy(e => e.OccurredAt)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => new EpisodicEvent
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    ConversationId = e.ConversationId,
                    EventType = e.EventType,
                    Content = e.Content,
                    OccurredAt = e.OccurredAt,
                    CreatedAt = e.CreatedAt
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite episodic events by time range");
            throw;
        }
    }

    public async Task<EpisodicEvent[]> GetEventsByTypeAsync(
        string userId,
        string eventType,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.EpisodicEvents
                .Where(e => e.UserId == userId && e.EventType == eventType)
                .OrderByDescending(e => e.OccurredAt)
                .Take(maxResults)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => new EpisodicEvent
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    ConversationId = e.ConversationId,
                    EventType = e.EventType,
                    Content = e.Content,
                    OccurredAt = e.OccurredAt,
                    CreatedAt = e.CreatedAt
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite episodic events by type");
            throw;
        }
    }

    public async Task<EpisodicEvent?> GetEventByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.EpisodicEvents
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (entity == null) return null;

            return new EpisodicEvent
            {
                Id = entity.Id,
                UserId = entity.UserId,
                ConversationId = entity.ConversationId,
                EventType = entity.EventType,
                Content = entity.Content,
                OccurredAt = entity.OccurredAt,
                CreatedAt = entity.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite episodic event by ID");
            throw;
        }
    }

    public async Task DeleteEventAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.EpisodicEvents
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted SQLite episodic event {EventId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SQLite episodic event");
            throw;
        }
    }

    public async Task<EpisodicEvent[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.EpisodicEvents
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => new EpisodicEvent
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    ConversationId = e.ConversationId,
                    EventType = e.EventType,
                    Content = e.Content,
                    OccurredAt = e.OccurredAt,
                    DecayFactor = e.DecayFactor,
                    CreatedAt = e.CreatedAt
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite episodic events for user");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.EpisodicEvents
                .Where(e => e.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all SQLite episodic memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from SQLite episodic memory");
            throw;
        }
    }

    public async Task<int> PromoteToProceduralAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Identify patterns: events that occur 3+ times within 30 days
            var eventGrouping = await _context.EpisodicEvents
                .Where(e => e.UserId == userId &&
                            e.OccurredAt > DateTime.UtcNow.AddDays(-30))
                .GroupBy(e => e.EventType)
                .Where(g => g.Count() >= 3)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            int promotedCount = eventGrouping.Count;

            _logger.LogInformation(
                "Identified {Count} patterns for promotion to SQLite procedural memory for user {UserId}",
                promotedCount, userId);

            return promotedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting SQLite episodic memory to procedural");
            throw;
        }
    }
}
