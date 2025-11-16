using MemoryKit.Domain.Enums;

namespace MemoryKit.Application.DTOs;

/// <summary>
/// DTO for creating a new message.
/// </summary>
public record CreateMessageRequest
{
    /// <summary>
    /// Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the message role.
    /// </summary>
    public required MessageRole Role { get; init; }

    /// <summary>
    /// Gets optional tags.
    /// </summary>
    public string[]? Tags { get; init; }
}

/// <summary>
/// DTO for message response.
/// </summary>
public record MessageResponse
{
    /// <summary>
    /// Gets the message ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the conversation ID.
    /// </summary>
    public required string ConversationId { get; init; }

    /// <summary>
    /// Gets the message role.
    /// </summary>
    public required MessageRole Role { get; init; }

    /// <summary>
    /// Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the importance score.
    /// </summary>
    public double ImportanceScore { get; init; }
}

/// <summary>
/// DTO for creating a new conversation.
/// </summary>
public record CreateConversationRequest
{
    /// <summary>
    /// Gets the conversation title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets optional tags.
    /// </summary>
    public string[]? Tags { get; init; }
}

/// <summary>
/// DTO for conversation response.
/// </summary>
public record ConversationResponse
{
    /// <summary>
    /// Gets the conversation ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the message count.
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// Gets the last activity timestamp.
    /// </summary>
    public required DateTime LastActivityAt { get; init; }
}

/// <summary>
/// DTO for querying memory and getting a response.
/// </summary>
public record QueryMemoryRequest
{
    /// <summary>
    /// Gets the user's question or query.
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// Gets the maximum tokens for the response.
    /// </summary>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>
    /// Gets a value indicating whether to include debug information.
    /// </summary>
    public bool IncludeDebugInfo { get; init; }
}

/// <summary>
/// DTO for memory query response.
/// </summary>
public record QueryMemoryResponse
{
    /// <summary>
    /// Gets the generated answer.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Gets the source information for the answer.
    /// </summary>
    public required MemorySource[] Sources { get; init; }

    /// <summary>
    /// Gets optional debug information.
    /// </summary>
    public DebugInfo? DebugInfo { get; init; }
}

/// <summary>
/// Represents a source of information for a query response.
/// </summary>
public record MemorySource
{
    /// <summary>
    /// Gets the type of memory source.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the content or reference.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the relevance score.
    /// </summary>
    public double RelevanceScore { get; init; }
}

/// <summary>
/// Debug information about query execution.
/// </summary>
public record DebugInfo
{
    /// <summary>
    /// Gets the query type that was detected.
    /// </summary>
    public required QueryType QueryType { get; init; }

    /// <summary>
    /// Gets the memory layers used.
    /// </summary>
    public required string[] LayersUsed { get; init; }

    /// <summary>
    /// Gets the total tokens used.
    /// </summary>
    public int TokensUsed { get; init; }

    /// <summary>
    /// Gets the retrieval time in milliseconds.
    /// </summary>
    public long RetrievalTimeMs { get; init; }
}
