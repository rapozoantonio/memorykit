using System.Text;
using System.Text.Json;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.Compression;
using MemoryKit.Infrastructure.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MemoryKit.Infrastructure.Azure;

/// <summary>
/// Azure Redis implementation of Working Memory Service.
/// </summary>
public class AzureRedisWorkingMemoryService : IWorkingMemoryService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<AzureRedisWorkingMemoryService> _logger;
    private readonly ICompressionService? _compressionService;
    private readonly int _maxItems;
    private readonly TimeSpan _conversationTtl;

    public AzureRedisWorkingMemoryService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<AzureRedisWorkingMemoryService> logger,
        ICompressionService? compressionService = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compressionService = compressionService;
        _db = redis.GetDatabase();

        // Configuration
        _maxItems = configuration.GetValue("MemoryKit:WorkingMemory:MaxItems", 10);
        _conversationTtl = TimeSpan.FromHours(
            configuration.GetValue("MemoryKit:WorkingMemory:TtlHours", 24));

        _logger.LogInformation(
            "AzureRedisWorkingMemoryService initialized (MaxItems: {MaxItems}, TTL: {Ttl})",
            _maxItems, _conversationTtl);
    }

    public async Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetKey(userId, conversationId);
            var json = SerializationHelper.Serialize(message);

            // Optionally compress if service is configured
            RedisValue value;
            if (_compressionService != null)
            {
                var compressed = await _compressionService.CompressAsync(json);
                value = compressed;
            }
            else
            {
                value = json;
            }

            // Store as a sorted set with timestamp as score
            await _db.SortedSetAddAsync(key, value, message.Timestamp.Ticks);

            // Keep only most recent MaxItems
            await _db.SortedSetRemoveRangeByRankAsync(key, 0, -_maxItems - 1);

            // Set expiration
            await _db.KeyExpireAsync(key, _conversationTtl);

            _logger.LogDebug(
                "Added message {MessageId} to Redis working memory: {Key}",
                message.Id, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to add message to Redis working memory for user {UserId}, conversation {ConversationId}",
                userId, conversationId);
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
            var key = GetKey(userId, conversationId);

            // Get most recent items (highest scores)
            var values = await _db.SortedSetRangeByRankAsync(key, -count, -1);

            var messages = new List<Message>();
            foreach (var value in values)
            {
                try
                {
                    string json;
                    if (_compressionService != null && value.HasValue)
                    {
                        // Try to decompress (may be compressed or uncompressed)
                        var bytes = (byte[])value!;
                        json = await _compressionService.DecompressToStringAsync(bytes);
                    }
                    else
                    {
                        json = value.ToString();
                    }

                    var message = SerializationHelper.Deserialize<Message>(json);
                    if (message != null)
                        messages.Add(message);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize message from Redis");
                }
            }

            // Refresh TTL on access
            await _db.KeyExpireAsync(key, _conversationTtl);

            _logger.LogDebug(
                "Retrieved {Count} messages from Redis working memory: {Key}",
                messages.Count, key);

            return messages.OrderBy(m => m.Timestamp).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve messages from Redis working memory for user {UserId}, conversation {ConversationId}",
                userId, conversationId);
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
            var key = GetKey(userId, conversationId);
            await _db.KeyDeleteAsync(key);

            _logger.LogDebug("Cleared Redis working memory: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to clear Redis working memory for user {UserId}, conversation {ConversationId}",
                userId, conversationId);
            throw;
        }
    }

    public async Task DeleteUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = $"wm:{userId}:*";
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            var keys = server.Keys(pattern: pattern);
            foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }

            _logger.LogInformation(
                "Deleted all Redis working memory for user {UserId}",
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete Redis working memory for user {UserId}",
                userId);
            throw;
        }
    }

    private static string GetKey(string userId, string conversationId)
        => $"wm:{userId}:{conversationId}";
}
