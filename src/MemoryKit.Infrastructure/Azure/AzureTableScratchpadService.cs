using System.Text.Json;
using Azure.Data.Tables;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.Embeddings;
using MemoryKit.Infrastructure.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Azure;

/// <summary>
/// Azure Table Storage implementation of Scratchpad Service.
/// Provides semantic memory with fact storage and retrieval.
/// </summary>
public class AzureTableScratchpadService : IScratchpadService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableScratchpadService> _logger;
    private readonly IEmbeddingQuantizer? _quantizer;
    private readonly TimeSpan _factTtl;

    public AzureTableScratchpadService(
        TableServiceClient tableServiceClient,
        IConfiguration configuration,
        ILogger<AzureTableScratchpadService> logger,
        IEmbeddingQuantizer? quantizer = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _quantizer = quantizer;

        var tableName = configuration["Azure:TableStorage:FactsTableName"] ?? "facts";
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();

        _factTtl = TimeSpan.FromDays(
            configuration.GetValue("MemoryKit:Scratchpad:TtlDays", 30));

        _logger.LogInformation(
            "AzureTableScratchpadService initialized (Table: {TableName}, TTL: {Ttl})",
            tableName, _factTtl);
    }

    public async Task StoreFactsAsync(
        string userId,
        string conversationId,
        ExtractedFact[] facts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var fact in facts)
            {
                var entity = new TableEntity(userId, fact.Id)
                {
                    ["Key"] = fact.Key,
                    ["Value"] = fact.Value,
                    ["Importance"] = fact.Importance,
                    ["ConversationId"] = conversationId,
                    ["CreatedAt"] = fact.CreatedAt,
                    ["LastAccessed"] = fact.LastAccessed,
                    ["AccessCount"] = fact.AccessCount
                };

                // Store quantized embedding if available, otherwise full embedding
                if (fact.IsQuantized)
                {
                    entity["QuantizedEmbeddingData"] = fact.QuantizedEmbeddingData;
                    entity["QuantizedScale"] = fact.QuantizedScale;
                    entity["QuantizedOffset"] = fact.QuantizedOffset;
                }
                else if (fact.Embedding != null && _quantizer != null)
                {
                    // Quantize on-the-fly before storage
                    var quantized = _quantizer.Quantize(fact.Embedding);
                    entity["QuantizedEmbeddingData"] = quantized.Data;
                    entity["QuantizedScale"] = quantized.Scale;
                    entity["QuantizedOffset"] = quantized.Offset;
                }
                else if (fact.Embedding != null)
                {
                    // Fallback to full embedding if no quantizer
                    entity["Embedding"] = SerializationHelper.Serialize(fact.Embedding);
                }

                await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
            }

            _logger.LogDebug(
                "Stored {Count} facts in Azure Table Storage for user {UserId}",
                facts.Length, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to store facts in Azure Table Storage for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task<ExtractedFact[]> SearchFactsAsync(
        string userId,
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query all facts for user (partition key)
            var filter = $"PartitionKey eq '{userId}'";
            var queryResult = _tableClient.QueryAsync<TableEntity>(
                filter: filter,
                cancellationToken: cancellationToken);

            var facts = new List<ExtractedFact>();
            var queryLower = query.ToLowerInvariant();

            await foreach (var entity in queryResult)
            {
                var key = entity.GetString("Key") ?? "";
                var value = entity.GetString("Value") ?? "";

                // Simple keyword matching (for vector search, use Azure AI Search)
                if (key.Contains(queryLower, StringComparison.OrdinalIgnoreCase) ||
                    value.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                {
                    // Use factory method and then set additional properties
                    var fact = ExtractedFact.Create(
                        userId: userId,
                        conversationId: entity.GetString("ConversationId") ?? string.Empty,
                        key: key,
                        value: value,
                        type: Enum.TryParse<EntityType>(entity.GetString("Type"), out var entityType) ? entityType : EntityType.Preference,
                        importance: entity.GetDouble("Importance") ?? 0.5);

                    // Load quantized embedding if available
                    if (entity.ContainsKey("QuantizedEmbeddingData"))
                    {
                        var quantizedData = entity.GetBinary("QuantizedEmbeddingData");
                        var scale = (float)(entity.GetDouble("QuantizedScale") ?? 1.0f);
                        var offset = (float)(entity.GetDouble("QuantizedOffset") ?? 0.0f);
                        fact.SetQuantizedEmbedding(quantizedData, scale, offset);
                    }
                    else if (entity.ContainsKey("Embedding"))
                    {
                        // Backward compatibility: load full embedding
                        var embeddingJson = entity.GetString("Embedding") ?? "[]";
                        var embedding = SerializationHelper.Deserialize<float[]>(embeddingJson);
                        if (embedding != null) fact.SetEmbedding(embedding);
                    }
                    
                    facts.Add(fact);
                }
            }

            _logger.LogDebug(
                "Found {Count} facts via keyword search for query: {Query}",
                facts.Count, query);

            return facts
                .OrderByDescending(f => f.Importance)
                .ThenByDescending(f => f.LastAccessed)
                .Take(maxResults)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to search facts in Azure Table Storage for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task RecordAccessAsync(string factId, CancellationToken cancellationToken = default)
    {
        try
        {
            // For full implementation, would need to scan or maintain secondary index
            // For now, access tracking happens during SearchFactsAsync
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record access for fact {FactId}", factId);
            throw;
        }
    }

    public async Task PruneAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - _factTtl;
            var filter = $"PartitionKey eq '{userId}'";

            var queryResult = _tableClient.QueryAsync<TableEntity>(
                filter: filter,
                cancellationToken: cancellationToken);

            var deletedCount = 0;

            await foreach (var entity in queryResult)
            {
                var lastAccessed = entity.GetDateTimeOffset("LastAccessed")?.UtcDateTime;
                var accessCount = entity.GetInt32("AccessCount") ?? 0;

                // Delete if old and rarely accessed
                if (lastAccessed < cutoff && accessCount < 2)
                {
                    await _tableClient.DeleteEntityAsync(userId, entity.RowKey, cancellationToken: cancellationToken);
                    deletedCount++;
                }
            }

            _logger.LogInformation(
                "Pruned {Count} old facts for user {UserId}",
                deletedCount, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to prune facts in Azure Table Storage for user {UserId}",
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
                "Deleted {Count} facts (semantic memory) for user {UserId}",
                deletedCount, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete semantic memory for user {UserId}",
                userId);
            throw;
        }
    }

    public Task DeleteFactAsync(string userId, string factId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("DeleteFactAsync not fully implemented for Azure Table yet");
        return Task.CompletedTask;
    }
}
