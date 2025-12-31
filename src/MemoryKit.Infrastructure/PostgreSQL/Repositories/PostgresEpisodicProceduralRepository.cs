using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Infrastructure.PostgreSQL.Repositories;

/// <summary>
/// PostgreSQL implementation of Episodic Memory repository.
/// </summary>
public class PostgresEpisodicMemoryRepository : IEpisodicMemoryRepository
{
    private readonly IDbContextFactory<MemoryKitDbContext> _contextFactory;
    private readonly ILogger<PostgresEpisodicMemoryRepository> _logger;

    public PostgresEpisodicMemoryRepository(
        IDbContextFactory<MemoryKitDbContext> contextFactory,
        ILogger<PostgresEpisodicMemoryRepository> logger)
    {
        _contextFactory = contextFactory;
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
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

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

            context.EpisodicEvents.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Added event {EventId} to episodic memory. Type: {EventType}, UserId: {UserId}",
                entity.Id, eventType, userId);

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding event to episodic memory");
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
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entities = await context.EpisodicEvents
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
            _logger.LogError(ex, "Error retrieving episodic events by time range");
            throw;
        }
    }

    public async Task<EpisodicEvent[]> GetEventsByTypeAsync(
        string userId,
        string eventType,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entities = await context.EpisodicEvents
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
            _logger.LogError(ex, "Error retrieving episodic events by type");
            throw;
        }
    }

    public async Task<EpisodicEvent?> GetEventByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entity = await context.EpisodicEvents
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
            _logger.LogError(ex, "Error retrieving episodic event by ID");
            throw;
        }
    }

    public async Task DeleteEventAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            await context.EpisodicEvents
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted episodic event {EventId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting episodic event");
            throw;
        }
    }

    public async Task<EpisodicEvent[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entities = await context.EpisodicEvents
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
            _logger.LogError(ex, "Error retrieving episodic events for user");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            await context.EpisodicEvents
                .Where(e => e.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all episodic memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from episodic memory");
            throw;
        }
    }

    public async Task<int> PromoteToProceduralAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            // Identify patterns: events that occur 3+ times within 30 days
            var eventGrouping = await context.EpisodicEvents
                .Where(e => e.UserId == userId &&
                            e.OccurredAt > DateTime.UtcNow.AddDays(-30))
                .GroupBy(e => e.EventType)
                .Where(g => g.Count() >= 3)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            int promotedCount = eventGrouping.Count;

            _logger.LogInformation(
                "Identified {Count} patterns for promotion to procedural memory for user {UserId}",
                promotedCount, userId);

            return promotedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting episodic memory to procedural");
            throw;
        }
    }
}

/// <summary>
/// PostgreSQL implementation of Procedural Memory repository.
/// </summary>
public class PostgresProceduralMemoryRepository : IProceduralMemoryRepository
{
    private readonly IDbContextFactory<MemoryKitDbContext> _contextFactory;
    private readonly ILogger<PostgresProceduralMemoryRepository> _logger;

    public PostgresProceduralMemoryRepository(
        IDbContextFactory<MemoryKitDbContext> contextFactory,
        ILogger<PostgresProceduralMemoryRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<string> AddPatternAsync(
        string userId,
        string patternName,
        string triggerConditions,
        string learnedResponse,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entity = new ProceduralPatternEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                PatternName = patternName,
                TriggerConditions = triggerConditions,
                LearnedResponse = learnedResponse,
                SuccessCount = 0,
                FailureCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.ProceduralPatterns.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Added pattern {PatternId} to procedural memory. Name: {PatternName}, UserId: {UserId}",
                entity.Id, patternName, userId);

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding pattern to procedural memory");
            throw;
        }
    }

    public async Task UpdatePatternAsync(ProceduralPattern pattern, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entity = await context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == pattern.Id, cancellationToken);

            if (entity != null)
            {
                entity.LearnedResponse = pattern.InstructionTemplate;
                entity.UpdatedAt = DateTime.UtcNow;

                context.ProceduralPatterns.Update(entity);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Updated pattern {PatternId} in procedural memory", pattern.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pattern in procedural memory");
            throw;
        }
    }

    public async Task<ProceduralPattern?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entity = await context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (entity == null) return null;

            return ProceduralPattern.Create(
                entity.UserId,
                entity.PatternName,
                entity.LearnedResponse, // description
                Array.Empty<PatternTrigger>(), // triggers
                entity.LearnedResponse); // instructionTemplate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving procedural pattern by ID");
            throw;
        }
    }

    public async Task<ProceduralPattern[]> GetByNameAsync(
        string userId,
        string patternName,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entities = await context.ProceduralPatterns
                .Where(p => p.UserId == userId && p.PatternName == patternName)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => ProceduralPattern.Create(e.UserId, e.PatternName, e.LearnedResponse, Array.Empty<PatternTrigger>(), e.LearnedResponse))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving procedural patterns by name");
            throw;
        }
    }

    public async Task<ProceduralPattern[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entities = await context.ProceduralPatterns
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.SuccessCount)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => ProceduralPattern.Create(e.UserId, e.PatternName, e.LearnedResponse, Array.Empty<PatternTrigger>(), e.LearnedResponse))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving procedural patterns for user");
            throw;
        }
    }

    public async Task<ProceduralPattern[]> FindByTriggersAsync(
        string userId,
        string triggerConditions,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entities = await context.ProceduralPatterns
                .Where(p => p.UserId == userId && p.TriggerConditions.Contains(triggerConditions))
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => ProceduralPattern.Create(e.UserId, e.PatternName, e.LearnedResponse, Array.Empty<PatternTrigger>(), e.LearnedResponse))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding procedural patterns by triggers");
            throw;
        }
    }

    public async Task RecordSuccessAsync(string patternId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entity = await context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == patternId, cancellationToken);

            if (entity != null)
            {
                entity.SuccessCount++;
                entity.LastUsedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                context.ProceduralPatterns.Update(entity);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Recorded success for pattern {PatternId}", patternId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording pattern success");
            throw;
        }
    }

    public async Task RecordFailureAsync(string patternId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var entity = await context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == patternId, cancellationToken);

            if (entity != null)
            {
                entity.FailureCount++;
                entity.LastUsedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                context.ProceduralPatterns.Update(entity);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Recorded failure for pattern {PatternId}", patternId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording pattern failure");
            throw;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            await context.ProceduralPatterns
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted procedural pattern {PatternId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting procedural pattern");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            await context.ProceduralPatterns
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all procedural memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from procedural memory");
            throw;
        }
    }
}
