using MemoryKit.Domain.Entities;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Infrastructure.SemanticKernel;

/// <summary>
/// Semantic Kernel Service - integrates with Azure OpenAI for embeddings and LLM operations.
/// </summary>
public interface ISemanticKernelService
{
    /// <summary>
    /// Generates an embedding vector for text.
    /// </summary>
    Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies the type of query.
    /// </summary>
    Task<string> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts entities from text.
    /// </summary>
    Task<ExtractedEntity[]> ExtractEntitiesAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a prompt using the configured LLM.
    /// </summary>
    Task<string> CompleteAsync(
        string prompt,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an answer with provided context.
    /// </summary>
    Task<string> AnswerWithContextAsync(
        string query,
        MemoryContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes the sentiment of text.
    /// </summary>
    Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(
        string text,
        CancellationToken cancellationToken = default);
}
