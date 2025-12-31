using MemoryKit.Domain.Entities;
using MemoryKit.Infrastructure.PostgreSQL;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.SQLite.Repositories;

/// <summary>
/// SQLite implementation of Procedural Memory repository.
/// Stores learned patterns and behaviors in local SQLite database.
/// </summary>
public class SqliteProceduralMemoryRepository : IProceduralMemoryRepository
{
    private readonly MemoryKitDbContext _context;
    private readonly ILogger<SqliteProceduralMemoryRepository> _logger;

    public SqliteProceduralMemoryRepository(
        MemoryKitDbContext context,
        ILogger<SqliteProceduralMemoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> AddPatternAsync(
        string userId,
        string patternName,
        string triggerConditions,
        string learnedResponse,
        CancellationToken cancellationToken = default)
    {
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

            _context.ProceduralPatterns.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Added pattern {PatternId} to SQLite procedural memory. Name: {PatternName}, UserId: {UserId}",
                entity.Id, patternName, userId);

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding pattern to SQLite procedural memory");
            throw;
        }
    }

    public async Task UpdatePatternAsync(ProceduralPattern pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == pattern.Id, cancellationToken);

            if (entity != null)
            {
                // Update only the fields that are mutable
                entity.UpdatedAt = DateTime.UtcNow;

                _context.ProceduralPatterns.Update(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Updated pattern {PatternId} in SQLite procedural memory", pattern.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pattern in SQLite procedural memory");
            throw;
        }
    }

    public async Task<ProceduralPattern?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (entity == null) return null;

            return ProceduralPattern.Create(
                entity.UserId, 
                entity.PatternName, 
                entity.LearnedResponse, // description
                Array.Empty<PatternTrigger>(), // triggers - will be populated from TriggerConditions
                entity.LearnedResponse); // instructionTemplate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite procedural pattern by ID");
            throw;
        }
    }

    public async Task<ProceduralPattern[]> GetByNameAsync(
        string userId,
        string patternName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.ProceduralPatterns
                .Where(p => p.UserId == userId && p.PatternName == patternName)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => ProceduralPattern.Create(e.UserId, e.PatternName, e.LearnedResponse, Array.Empty<PatternTrigger>(), e.LearnedResponse))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite procedural patterns by name");
            throw;
        }
    }

    public async Task<ProceduralPattern[]> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.ProceduralPatterns
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.LastUsedAt ?? p.CreatedAt)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => ProceduralPattern.Create(e.UserId, e.PatternName, e.LearnedResponse, Array.Empty<PatternTrigger>(), e.LearnedResponse))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SQLite procedural patterns for user");
            throw;
        }
    }

    public async Task<ProceduralPattern[]> FindByTriggersAsync(
        string userId,
        string triggerConditions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Text-based trigger matching (SQLite doesn't support JSON query like PostgreSQL)
            var entities = await _context.ProceduralPatterns
                .Where(p => p.UserId == userId && p.TriggerConditions.Contains(triggerConditions))
                .OrderByDescending(p => p.LastUsedAt ?? p.CreatedAt)
                .ToListAsync(cancellationToken);

            return entities
                .Select(e => ProceduralPattern.Create(e.UserId, e.PatternName, e.LearnedResponse, Array.Empty<PatternTrigger>(), e.LearnedResponse))
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding SQLite procedural patterns by triggers");
            throw;
        }
    }

    public async Task RecordSuccessAsync(string patternId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == patternId, cancellationToken);

            if (entity != null)
            {
                entity.SuccessCount++;
                entity.LastUsedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.ProceduralPatterns.Update(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Recorded success for pattern {PatternId} in SQLite procedural memory", patternId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording success in SQLite procedural memory");
            throw;
        }
    }

    public async Task RecordFailureAsync(string patternId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.ProceduralPatterns
                .FirstOrDefaultAsync(p => p.Id == patternId, cancellationToken);

            if (entity != null)
            {
                entity.FailureCount++;
                entity.LastUsedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.ProceduralPatterns.Update(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Recorded failure for pattern {PatternId} in SQLite procedural memory", patternId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failure in SQLite procedural memory");
            throw;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.ProceduralPatterns
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted SQLite procedural pattern {PatternId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SQLite procedural pattern");
            throw;
        }
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.ProceduralPatterns
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted all SQLite procedural memory for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user data from SQLite procedural memory");
            throw;
        }
    }
}
