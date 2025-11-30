using System.Text.Json;
using Azure.Data.Tables;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Azure;

/// <summary>
/// Azure Table Storage implementation of Procedural Memory Service.
/// Stores learned patterns and routines.
/// </summary>
public class AzureTableProceduralMemoryService : IProceduralMemoryService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableProceduralMemoryService> _logger;

    public AzureTableProceduralMemoryService(
        TableServiceClient tableServiceClient,
        IConfiguration configuration,
        ILogger<AzureTableProceduralMemoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var tableName = configuration["Azure:TableStorage:PatternsTableName"] ?? "patterns";
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();

        _logger.LogInformation(
            "AzureTableProceduralMemoryService initialized (Table: {TableName})",
            tableName);
    }

    public async Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = $"PartitionKey eq '{userId}'";
            var queryResult = _tableClient.QueryAsync<TableEntity>(
                filter: filter,
                cancellationToken: cancellationToken);

            var queryLower = query.ToLowerInvariant();
            ProceduralPattern? bestMatch = null;
            double bestScore = 0.0;

            await foreach (var entity in queryResult)
            {
                var patternJson = entity.GetString("Pattern") ?? "{}";
                var pattern = JsonSerializer.Deserialize<ProceduralPattern>(patternJson);

                if (pattern != null)
                {
                    // Simple keyword matching
                    var matchScore = CalculateMatchScore(query, pattern);
                    if (matchScore > bestScore)
                    {
                        bestScore = matchScore;
                        bestMatch = pattern;
                    }
                }
            }

            if (bestMatch != null && bestScore > 0.5)
            {
                _logger.LogDebug(
                    "Matched pattern '{PatternName}' with score {Score} for query: {Query}",
                    bestMatch.Name, bestScore, query);
                return bestMatch;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to match pattern for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task StorePatternAsync(
        ProceduralPattern pattern,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new TableEntity(pattern.UserId, pattern.Id)
            {
                ["Name"] = pattern.Name,
                ["Pattern"] = JsonSerializer.Serialize(pattern),
                ["CreatedAt"] = pattern.CreatedAt,
                ["LastUsed"] = pattern.LastUsed,
                ["UsageCount"] = pattern.UsageCount
            };

            await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Stored pattern '{PatternName}' for user {UserId}",
                pattern.Name, pattern.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to store pattern '{PatternName}' for user {UserId}",
                pattern.Name, pattern.UserId);
            throw;
        }
    }

    public async Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple pattern detection based on message content
            var content = message.Content.ToLowerInvariant();

            // Detect common patterns
            if (content.Contains("write", StringComparison.OrdinalIgnoreCase) || 
                content.Contains("create", StringComparison.OrdinalIgnoreCase) || 
                content.Contains("generate", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = ProceduralPattern.Create(
                    userId: userId,
                    name: "ContentGeneration",
                    description: "Auto-detected content generation pattern",
                    triggers: new[]
                    {
                        new PatternTrigger
                        {
                            Type = Domain.Enums.TriggerType.Keyword,
                            Pattern = "write"
                        },
                        new PatternTrigger
                        {
                            Type = Domain.Enums.TriggerType.Keyword,
                            Pattern = "create"
                        },
                        new PatternTrigger
                        {
                            Type = Domain.Enums.TriggerType.Keyword,
                            Pattern = "generate"
                        }
                    },
                    instructionTemplate: "Follow the user's content generation request carefully.",
                    confidenceThreshold: 0.75
                );

                await StorePatternAsync(pattern, cancellationToken);
            }

            _logger.LogDebug(
                "Detected and stored pattern for user {UserId} from message {MessageId}",
                userId, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to detect and store pattern for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task<ProceduralPattern[]> GetUserPatternsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = $"PartitionKey eq '{userId}'";
            var queryResult = _tableClient.QueryAsync<TableEntity>(
                filter: filter,
                cancellationToken: cancellationToken);

            var patterns = new List<ProceduralPattern>();

            await foreach (var entity in queryResult)
            {
                var patternJson = entity.GetString("Pattern") ?? "{}";
                var pattern = JsonSerializer.Deserialize<ProceduralPattern>(patternJson);

                if (pattern != null)
                    patterns.Add(pattern);
            }

            _logger.LogDebug(
                "Retrieved {Count} patterns for user {UserId}",
                patterns.Count, userId);

            return patterns.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve patterns for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = $"PartitionKey eq '{userId}'";
            var queryResult = _tableClient.QueryAsync<TableEntity>(
                filter: filter,
                cancellationToken: cancellationToken);

            var deletedCount = 0;

            await foreach (var entity in queryResult)
            {
                await _tableClient.DeleteEntityAsync(userId, entity.RowKey, cancellationToken: cancellationToken);
                deletedCount++;
            }

            _logger.LogInformation(
                "Deleted {Count} patterns (procedural memory) for user {UserId}",
                deletedCount, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete procedural memory for user {UserId}",
                userId);
            throw;
        }
    }

    private double CalculateMatchScore(string query, ProceduralPattern pattern)
    {
        var queryLower = query.ToLowerInvariant();
        var matchCount = pattern.Triggers.Count(trigger =>
            queryLower.Contains(trigger.Pattern, StringComparison.OrdinalIgnoreCase));

        return pattern.Triggers.Length > 0
            ? (double)matchCount / pattern.Triggers.Length
            : 0.0;
    }
}
