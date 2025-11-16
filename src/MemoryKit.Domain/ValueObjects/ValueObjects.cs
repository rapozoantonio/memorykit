using MemoryKit.Domain.Enums;

namespace MemoryKit.Domain.ValueObjects;

/// <summary>
/// Represents the importance score of a message using the Amygdala model.
/// </summary>
public record ImportanceScore
{
    /// <summary>
    /// Gets the base importance score (0.0 to 1.0).
    /// </summary>
    public double BaseScore { get; init; }

    /// <summary>
    /// Gets the emotional weight contribution (0.0 to 1.0).
    /// </summary>
    public double EmotionalWeight { get; init; }

    /// <summary>
    /// Gets the novelty boost for new information (0.0 to 1.0).
    /// </summary>
    public double NoveltyBoost { get; init; }

    /// <summary>
    /// Gets the recency factor with time decay (0.0 to 1.0).
    /// </summary>
    public double RecencyFactor { get; init; }

    /// <summary>
    /// Gets the final calculated importance score.
    /// Combines all factors with weighted algorithm.
    /// </summary>
    public double FinalScore => Math.Clamp(
        (BaseScore * 0.4) +
        (EmotionalWeight * 0.3) +
        (NoveltyBoost * 0.2) +
        (RecencyFactor * 0.1),
        0.0,
        1.0);
}

/// <summary>
/// Represents an embedding vector for semantic search.
/// </summary>
public record EmbeddingVector
{
    /// <summary>
    /// Gets the vector data.
    /// </summary>
    public required float[] Vector { get; init; }

    /// <summary>
    /// Gets the model used to generate this embedding.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Gets the dimension of the embedding vector.
    /// </summary>
    public int Dimension => Vector.Length;

    /// <summary>
    /// Calculates the cosine similarity between this vector and another.
    /// </summary>
    public double CosineSimilarity(float[] other)
    {
        if (Vector.Length != other.Length)
            throw new ArgumentException("Vector dimensions must match");

        var dotProduct = Vector.Zip(other, (a, b) => a * b).Sum();
        var magnitudeA = Math.Sqrt(Vector.Sum(x => x * x));
        var magnitudeB = Math.Sqrt(other.Sum(x => x * x));

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0.0;

        return dotProduct / (magnitudeA * magnitudeB);
    }
}

/// <summary>
/// Represents a query execution plan that determines which memory layers to retrieve from.
/// </summary>
public record QueryPlan
{
    /// <summary>
    /// Gets the type of query being executed.
    /// </summary>
    public required QueryType Type { get; init; }

    /// <summary>
    /// Gets the memory layers to retrieve from for this query.
    /// </summary>
    public required List<MemoryLayer> LayersToUse { get; init; }

    /// <summary>
    /// Gets the suggested procedural pattern, if any.
    /// </summary>
    public string? SuggestedProcedureId { get; init; }

    /// <summary>
    /// Gets the estimated token count for this query plan.
    /// </summary>
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// Gets a value indicating whether this query should include historical context.
    /// </summary>
    public bool IncludeHistoricalContext { get; init; }
}
