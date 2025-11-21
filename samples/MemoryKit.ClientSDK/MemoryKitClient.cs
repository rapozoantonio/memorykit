using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemoryKit.ClientSDK;

/// <summary>
/// Type-safe client SDK for MemoryKit API
/// </summary>
public class MemoryKitClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Base URL of the MemoryKit API
    /// </summary>
    public string BaseUrl { get; }

    public MemoryKitClient(string baseUrl, HttpClient? httpClient = null)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(BaseUrl);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    #region Conversation Management

    /// <summary>
    /// Create a new conversation
    /// </summary>
    public async Task<ConversationResponse> CreateConversationAsync(
        string conversationId,
        string userId,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateConversationRequest
        {
            ConversationId = conversationId,
            UserId = userId,
            Metadata = metadata
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/conversations",
            request,
            _jsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConversationResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    /// <summary>
    /// Get conversation details
    /// </summary>
    public async Task<ConversationResponse> GetConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/api/v1/conversations/{conversationId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConversationResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    #endregion

    #region Message Management

    /// <summary>
    /// Add a message to a conversation
    /// </summary>
    public async Task<MessageResponse> AddMessageAsync(
        string conversationId,
        string userId,
        string content,
        bool isUserMessage = true,
        CancellationToken cancellationToken = default)
    {
        var request = new AddMessageRequest
        {
            ConversationId = conversationId,
            UserId = userId,
            Content = content,
            IsUserMessage = isUserMessage
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/messages",
            request,
            _jsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    /// <summary>
    /// Query conversation context
    /// </summary>
    public async Task<ContextResponse> QueryContextAsync(
        string conversationId,
        string userId,
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var request = new QueryContextRequest
        {
            ConversationId = conversationId,
            UserId = userId,
            Query = query,
            TopK = topK
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/context/query",
            request,
            _jsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContextResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    #endregion

    #region Metrics & Health

    /// <summary>
    /// Get performance metrics
    /// </summary>
    public async Task<PerformanceMetricsResponse> GetPerformanceMetricsAsync(
        int windowMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/api/v1/metrics/performance?windowMinutes={windowMinutes}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PerformanceMetricsResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    /// <summary>
    /// Get health status
    /// </summary>
    public async Task<HealthResponse> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "/api/v1/metrics/health",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HealthResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    /// <summary>
    /// Record custom metric
    /// </summary>
    public async Task RecordMetricAsync(
        string operationName,
        double latencyMs,
        CancellationToken cancellationToken = default)
    {
        var request = new RecordMetricRequest
        {
            OperationName = operationName,
            LatencyMs = latencyMs
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/metrics/record",
            request,
            _jsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Process multiple messages in batch
    /// </summary>
    public async Task<List<MessageResponse>> AddMessagesBatchAsync(
        string conversationId,
        string userId,
        IEnumerable<string> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MessageResponse>();
        
        foreach (var message in messages)
        {
            var result = await AddMessageAsync(conversationId, userId, message, true, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
    }

    #endregion
}

#region Request/Response Models

public class CreateConversationRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}

public class AddMessageRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsUserMessage { get; set; } = true;
}

public class QueryContextRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
}

public class RecordMetricRequest
{
    public string OperationName { get; set; } = string.Empty;
    public double LatencyMs { get; set; }
}

public class ConversationResponse
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Metadata { get; set; }
}

public class MessageResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double CalculatedImportance { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsUserMessage { get; set; }
}

public class ContextResponse
{
    public List<MemoryItem> RelevantMemories { get; set; } = new();
    public string AssembledContext { get; set; } = string.Empty;
    public int TotalRetrieved { get; set; }
}

public class MemoryItem
{
    public string Content { get; set; } = string.Empty;
    public double Importance { get; set; }
    public DateTime Timestamp { get; set; }
    public double RelevanceScore { get; set; }
}

public class PerformanceMetricsResponse
{
    public int TotalOperations { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P50LatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double OperationsPerSecond { get; set; }
    public Dictionary<string, OperationStats> OperationBreakdown { get; set; } = new();
}

public class OperationStats
{
    public int Count { get; set; }
    public double AverageMs { get; set; }
    public double MinMs { get; set; }
    public double MaxMs { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public double P95LatencyMs { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion
