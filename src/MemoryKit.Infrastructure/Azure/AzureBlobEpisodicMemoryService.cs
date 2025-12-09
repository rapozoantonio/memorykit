using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.Compression;
using MemoryKit.Infrastructure.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Azure;

/// <summary>
/// Azure Blob Storage + AI Search implementation of Episodic Memory Service.
/// Provides full conversation archive with vector search capabilities.
/// </summary>
public class AzureBlobEpisodicMemoryService : IEpisodicMemoryService
{
    private readonly BlobContainerClient _containerClient;
    private readonly SearchClient _searchClient;
    private readonly ILogger<AzureBlobEpisodicMemoryService> _logger;
    private readonly ICompressionService? _compressionService;

    public AzureBlobEpisodicMemoryService(
        BlobServiceClient blobServiceClient,
        SearchClient searchClient,
        IConfiguration configuration,
        ILogger<AzureBlobEpisodicMemoryService> logger,
        ICompressionService? compressionService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compressionService = compressionService;

        var containerName = configuration["Azure:Storage:ContainerName"] ?? "conversations";
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();

        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));

        _logger.LogInformation(
            "AzureBlobEpisodicMemoryService initialized (Container: {ContainerName})",
            containerName);
    }

    public async Task ArchiveAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Store full message in Blob
            var blobName = $"{message.UserId}/{message.ConversationId}/{message.Id}.json";
            var blobClient = _containerClient.GetBlobClient(blobName);

            var json = SerializationHelper.Serialize(message);
            
            // Compress before uploading to blob storage (Brotli for best compression ratio)
            BinaryData data;
            if (_compressionService != null)
            {
                var compressed = await _compressionService.CompressAsync(json, cancellationToken);
                data = BinaryData.FromBytes(compressed);
            }
            else
            {
                data = BinaryData.FromString(json);
            }

            await blobClient.UploadAsync(
                data,
                overwrite: true,
                cancellationToken: cancellationToken);

            // 2. Index searchable fields in AI Search
            var searchDoc = new SearchDocument
            {
                ["id"] = message.Id,
                ["userId"] = message.UserId,
                ["conversationId"] = message.ConversationId,
                ["content"] = message.Content,
                ["role"] = message.Role.ToString(),
                ["timestamp"] = message.Timestamp,
                ["importance"] = message.Metadata.ImportanceScore,
                ["blobPath"] = blobName
                // Note: Vector embeddings would need to be added if AI Search vector search is required
            };

            await _searchClient.IndexDocumentsAsync(
                IndexDocumentsBatch.Upload(new[] { searchDoc }),
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Archived message {MessageId} to Blob and indexed in AI Search",
                message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to archive message {MessageId}",
                message.Id);
            throw;
        }
    }

    public async Task<Message[]> SearchAsync(
        string userId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                Filter = $"userId eq '{userId}'",
                Size = maxResults
            };

            searchOptions.OrderBy.Add("importance desc");
            searchOptions.OrderBy.Add("timestamp desc");

            var results = await _searchClient.SearchAsync<SearchDocument>(
                query,
                searchOptions,
                cancellationToken);

            var messages = new List<Message>();

            await foreach (var result in results.Value.GetResultsAsync())
            {
                var blobPath = result.Document["blobPath"]?.ToString();
                if (blobPath != null)
                {
                    var blobClient = _containerClient.GetBlobClient(blobPath);
                    
                    if (await blobClient.ExistsAsync(cancellationToken))
                    {
                        var content = await blobClient.DownloadContentAsync(cancellationToken);
                        
                        // Decompress if compression service is configured
                        string json;
                        if (_compressionService != null)
                        {
                            json = await _compressionService.DecompressToStringAsync(
                                content.Value.Content.ToArray(),
                                cancellationToken);
                        }
                        else
                        {
                            json = content.Value.Content.ToString();
                        }

                        var message = SerializationHelper.Deserialize<Message>(json);

                        if (message != null)
                            messages.Add(message);
                    }
                }
            }

            _logger.LogDebug(
                "Found {Count} messages via AI Search for query: {Query}",
                messages.Count, query);

            return messages.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to search episodic memory for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task<Message?> GetAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Search by ID to find blob path
            var filter = $"id eq '{messageId}'";
            var results = await _searchClient.SearchAsync<SearchDocument>(
                "*",
                new SearchOptions { Filter = filter },
                cancellationToken);

            await foreach (var result in results.Value.GetResultsAsync())
            {
                var blobPath = result.Document["blobPath"]?.ToString();
                if (blobPath != null)
                {
                    var blobClient = _containerClient.GetBlobClient(blobPath);

                    if (await blobClient.ExistsAsync(cancellationToken))
                    {
                        var content = await blobClient.DownloadContentAsync(cancellationToken);
                        
                        // Decompress if compression service is configured
                        string json;
                        if (_compressionService != null)
                        {
                            json = await _compressionService.DecompressToStringAsync(
                                content.Value.Content.ToArray(),
                                cancellationToken);
                        }
                        else
                        {
                            json = content.Value.Content.ToString();
                        }

                        return SerializationHelper.Deserialize<Message>(json);
                    }
                }
            }

            _logger.LogDebug("Message {MessageId} not found in episodic memory", messageId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get message {MessageId} from episodic memory",
                messageId);
            throw;
        }
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Delete from AI Search
            var filter = $"userId eq '{userId}'";
            var results = await _searchClient.SearchAsync<SearchDocument>(
                "*",
                new SearchOptions { Filter = filter, Size = 1000 },
                cancellationToken);

            var idsToDelete = new List<string>();
            await foreach (var result in results.Value.GetResultsAsync())
            {
                var id = result.Document["id"]?.ToString();
                if (id != null)
                    idsToDelete.Add(id);
            }

            if (idsToDelete.Any())
            {
                var deleteBatch = IndexDocumentsBatch.Delete("id", idsToDelete);
                await _searchClient.IndexDocumentsAsync(deleteBatch, cancellationToken: cancellationToken);
            }

            // 2. Delete from Blob Storage
            var prefix = $"{userId}/";
            var deletedBlobCount = 0;

            await foreach (var blobItem in _containerClient.GetBlobsAsync(
                prefix: prefix,
                cancellationToken: cancellationToken))
            {
                await _containerClient.DeleteBlobAsync(blobItem.Name, cancellationToken: cancellationToken);
                deletedBlobCount++;
            }

            _logger.LogInformation(
                "Deleted {IndexCount} search entries and {BlobCount} blobs (episodic memory) for user {UserId}",
                idsToDelete.Count, deletedBlobCount, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete episodic memory for user {UserId}",
                userId);
            throw;
        }
    }
}
