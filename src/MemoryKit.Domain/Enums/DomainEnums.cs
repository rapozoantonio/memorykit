namespace MemoryKit.Domain.Enums;

/// <summary>
/// Represents the role of a message in a conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Message from the user.
    /// </summary>
    User = 0,

    /// <summary>
    /// Message from the assistant/AI.
    /// </summary>
    Assistant = 1,

    /// <summary>
    /// System message with instructions or metadata.
    /// </summary>
    System = 2
}

/// <summary>
/// Represents the type of query being performed.
/// </summary>
public enum QueryType
{
    /// <summary>
    /// Continue the previous topic (minimal context needed).
    /// </summary>
    Continuation = 0,

    /// <summary>
    /// Retrieve specific facts or information.
    /// </summary>
    FactRetrieval = 1,

    /// <summary>
    /// Deep recall requiring exact quotes or detailed history.
    /// </summary>
    DeepRecall = 2,

    /// <summary>
    /// Complex multi-faceted question requiring analysis.
    /// </summary>
    Complex = 3,

    /// <summary>
    /// Task that matches a learned routine/procedure.
    /// </summary>
    ProceduralTrigger = 4
}

/// <summary>
/// Represents a memory layer in the system.
/// </summary>
public enum MemoryLayer
{
    /// <summary>
    /// Working memory (Redis) - hot context for active conversations.
    /// </summary>
    WorkingMemory = 0,

    /// <summary>
    /// Semantic memory (Table Storage) - extracted facts and entities.
    /// </summary>
    SemanticMemory = 1,

    /// <summary>
    /// Episodic memory (Blob + AI Search) - full conversation archive.
    /// </summary>
    EpisodicMemory = 2,

    /// <summary>
    /// Procedural memory - learned workflows and routines.
    /// </summary>
    ProceduralMemory = 3
}

/// <summary>
/// Represents the type of extracted entity.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// Person entity (name, role, etc.).
    /// </summary>
    Person = 0,

    /// <summary>
    /// Place entity (location, system, etc.).
    /// </summary>
    Place = 1,

    /// <summary>
    /// Technology entity (frameworks, languages, tools).
    /// </summary>
    Technology = 2,

    /// <summary>
    /// Decision or commitment.
    /// </summary>
    Decision = 3,

    /// <summary>
    /// User preference or style choice.
    /// </summary>
    Preference = 4,

    /// <summary>
    /// Constraint or limitation.
    /// </summary>
    Constraint = 5,

    /// <summary>
    /// Goal or objective.
    /// </summary>
    Goal = 6,

    /// <summary>
    /// Other unclassified entity.
    /// </summary>
    Other = 7
}

/// <summary>
/// Represents the type of trigger for procedural patterns.
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// Keyword-based trigger.
    /// </summary>
    Keyword = 0,

    /// <summary>
    /// Regular expression-based trigger.
    /// </summary>
    Regex = 1,

    /// <summary>
    /// Semantic similarity-based trigger.
    /// </summary>
    Semantic = 2
}
