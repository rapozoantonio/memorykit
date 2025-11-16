using MemoryKit.Domain.Common;
using MemoryKit.Domain.Enums;

namespace MemoryKit.Domain.Entities;

/// <summary>
/// Represents a message in a conversation.
/// </summary>
public class Message : Entity<string>
{
    /// <summary>
    /// Gets the user ID who sent or owns this message.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the conversation ID this message belongs to.
    /// </summary>
    public string ConversationId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the role of the message sender.
    /// </summary>
    public MessageRole Role { get; private set; }

    /// <summary>
    /// Gets the content of the message.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Gets the metadata associated with this message.
    /// </summary>
    public MessageMetadata Metadata { get; private set; } = new();

    /// <summary>
    /// Factory method to create a new message.
    /// </summary>
    public static Message Create(
        string userId,
        string conversationId,
        MessageRole role,
        string content)
    {
        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ConversationId = conversationId,
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            Metadata = MessageMetadata.Default()
        };

        return message;
    }

    /// <summary>
    /// Marks this message with an importance score.
    /// </summary>
    public void MarkAsImportant(double score)
    {
        Metadata = Metadata with { ImportanceScore = Math.Clamp(score, 0.0, 1.0) };
    }

    /// <summary>
    /// Updates the extracted entities for this message.
    /// </summary>
    public void SetExtractedEntities(params ExtractedEntity[] entities)
    {
        Metadata = Metadata with { ExtractedEntities = entities };
    }

    /// <summary>
    /// Marks this message as containing a question.
    /// </summary>
    public void MarkAsQuestion()
    {
        Metadata = Metadata with { IsUserQuestion = true };
    }

    /// <summary>
    /// Marks this message as containing a decision or commitment.
    /// </summary>
    public void MarkAsDecision()
    {
        Metadata = Metadata with { ContainsDecision = true };
    }

    /// <summary>
    /// Marks this message as containing code.
    /// </summary>
    public void MarkAsContainingCode()
    {
        Metadata = Metadata with { ContainsCode = true };
    }
}

/// <summary>
/// Metadata associated with a message.
/// </summary>
public record MessageMetadata
{
    /// <summary>
    /// Gets a value indicating whether the message is a user question.
    /// </summary>
    public bool IsUserQuestion { get; init; }

    /// <summary>
    /// Gets a value indicating whether the message contains a decision or commitment.
    /// </summary>
    public bool ContainsDecision { get; init; }

    /// <summary>
    /// Gets a value indicating whether the message contains code.
    /// </summary>
    public bool ContainsCode { get; init; }

    /// <summary>
    /// Gets the tags associated with this message.
    /// </summary>
    public string[] Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the importance score (0.0 to 1.0).
    /// </summary>
    public double ImportanceScore { get; init; }

    /// <summary>
    /// Gets the extracted entities from this message.
    /// </summary>
    public ExtractedEntity[] ExtractedEntities { get; init; } = Array.Empty<ExtractedEntity>();

    /// <summary>
    /// Creates a default metadata instance.
    /// </summary>
    public static MessageMetadata Default() => new();
}

/// <summary>
/// Represents an extracted entity from a message.
/// </summary>
public record ExtractedEntity
{
    /// <summary>
    /// Gets the key/name of the entity.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the value of the entity.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the type of the entity.
    /// </summary>
    public required EntityType Type { get; init; }

    /// <summary>
    /// Gets the importance score of this entity.
    /// </summary>
    public double Importance { get; init; } = 0.5;

    /// <summary>
    /// Gets a value indicating whether this is a newly discovered entity.
    /// </summary>
    public bool IsNovel { get; init; }

    /// <summary>
    /// Gets the embedding vector for semantic search.
    /// </summary>
    public float[]? Embedding { get; init; }
}
