using MemoryKit.Domain.Common;
using MemoryKit.Domain.Enums;

namespace MemoryKit.Domain.Entities;

/// <summary>
/// Represents a learned procedural pattern or routine.
/// </summary>
public class ProceduralPattern : Entity<string>
{
    private readonly object _recordUsageLock = new object();

    /// <summary>
    /// Gets the user ID who owns this pattern.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the pattern.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description of what this pattern does.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the triggers that activate this pattern.
    /// </summary>
    public PatternTrigger[] Triggers { get; private set; } = Array.Empty<PatternTrigger>();

    /// <summary>
    /// Gets the instruction template to apply when pattern is matched.
    /// </summary>
    public string InstructionTemplate { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the confidence threshold for pattern matching (0.0 to 1.0).
    /// </summary>
    public double ConfidenceThreshold { get; private set; } = 0.8;

    /// <summary>
    /// Gets the number of times this pattern has been used.
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Gets the last time this pattern was used.
    /// </summary>
    public DateTime LastUsed { get; private set; }

    /// <summary>
    /// Factory method to create a new procedural pattern.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when userId, name, description, or instructionTemplate is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when triggers is null.</exception>
    public static ProceduralPattern Create(
        string userId,
        string name,
        string description,
        PatternTrigger[] triggers,
        string instructionTemplate,
        double confidenceThreshold = 0.8)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or whitespace", nameof(description));

        if (triggers == null || triggers.Length == 0)
            throw new ArgumentException("Triggers cannot be null or empty", nameof(triggers));

        if (string.IsNullOrWhiteSpace(instructionTemplate))
            throw new ArgumentException("Instruction template cannot be null or whitespace", nameof(instructionTemplate));

        return new ProceduralPattern
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = name,
            Description = description,
            Triggers = triggers,
            InstructionTemplate = instructionTemplate,
            ConfidenceThreshold = Math.Clamp(confidenceThreshold, 0.0, 1.0),
            LastUsed = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Determines whether this pattern matches given query with specified similarity score.
    /// </summary>
    public bool Matches(string query, double similarity)
    {
        return similarity >= ConfidenceThreshold;
    }

    /// <summary>
    /// Records a usage of this pattern and applies reinforcement learning adjustments.
    /// Thread-safe implementation using lock for state modifications.
    /// </summary>
    public void RecordUsage()
    {
        lock (_recordUsageLock)
        {
            UsageCount++;
            LastUsed = DateTime.UtcNow;

            // Reinforcement learning: decrease confidence threshold with repeated successful usage
            if (UsageCount > 10 && ConfidenceThreshold > 0.7)
            {
                ConfidenceThreshold = Math.Max(0.6, ConfidenceThreshold - 0.05);
                UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Adds a trigger to this pattern.
    /// </summary>
    public void AddTrigger(PatternTrigger trigger)
    {
        Triggers = Triggers.Append(trigger).ToArray();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets all triggers for this pattern.
    /// </summary>
    public void SetTriggers(params PatternTrigger[] triggers)
    {
        Triggers = triggers;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the instruction template.
    /// </summary>
    public void UpdateInstructionTemplate(string newTemplate)
    {
        InstructionTemplate = newTemplate;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a trigger condition for activating a procedural pattern.
/// </summary>
public record PatternTrigger
{
    /// <summary>
    /// Gets the type of trigger.
    /// </summary>
    public required TriggerType Type { get; init; }

    /// <summary>
    /// Gets the pattern/condition string (keyword, regex, or semantic).
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the embedding vector for semantic triggers.
    /// </summary>
    public float[]? Embedding { get; init; }
}
