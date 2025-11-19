using MemoryKit.Domain.Common;

namespace MemoryKit.Domain.Entities;

/// <summary>
/// Represents a conversation between a user and the assistant.
/// </summary>
public class Conversation : Entity<string>
{
    /// <summary>
    /// Gets the user ID who owns this conversation.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the title or subject of the conversation.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description of the conversation.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the tags associated with this conversation.
    /// </summary>
    public string[] Tags { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Gets the message count in this conversation.
    /// </summary>
    public int MessageCount { get; private set; }

    /// <summary>
    /// Gets the last activity timestamp.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// Factory method to create a new conversation.
    /// </summary>
    public static Conversation Create(string userId, string title, string? description = null)
    {
        return new Conversation
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = title,
            Description = description,
            LastActivityAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Adds tags to the conversation.
    /// </summary>
    public void AddTags(params string[] tags)
    {
        Tags = Tags.Union(tags).ToArray();
    }

    /// <summary>
    /// Records a message addition to the conversation.
    /// </summary>
    public void RecordMessageAdded()
    {
        MessageCount++;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the conversation title.
    /// </summary>
    public void UpdateTitle(string newTitle)
    {
        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            Title = newTitle;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
