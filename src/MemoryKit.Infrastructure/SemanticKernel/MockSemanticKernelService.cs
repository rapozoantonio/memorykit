using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.SemanticKernel;

/// <summary>
/// Mock Semantic Kernel Service for MVP/testing without Azure OpenAI dependency.
/// Replace with real Azure OpenAI integration for production.
/// </summary>
public class MockSemanticKernelService : ISemanticKernelService
{
    private readonly ILogger<MockSemanticKernelService> _logger;
    private readonly Random _random = new();

    public MockSemanticKernelService(ILogger<MockSemanticKernelService> logger)
    {
        _logger = logger;
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network latency

        // Generate a simple hash-based embedding for consistency
        // In production, use actual Azure OpenAI embeddings
        var embedding = new float[1536]; // Standard dimension for text-embedding-ada-002

        var hash = text.GetHashCode();
        var random = new Random(hash);

        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Range: -1 to 1
        }

        // Normalize to unit vector
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] /= magnitude;
        }

        _logger.LogDebug("Generated mock embedding for text (length: {Length})", text.Length);

        return embedding;
    }

    public async Task<string> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);

        var queryLower = query.ToLowerInvariant();

        // Simple rule-based classification
        if (queryLower.StartsWith("continue") || queryLower.StartsWith("go on"))
        {
            return QueryType.Continuation.ToString();
        }

        if (queryLower.Contains("what was") || queryLower.Contains("who is") || queryLower.Contains("when did"))
        {
            return QueryType.FactRetrieval.ToString();
        }

        if (queryLower.Contains("quote") || queryLower.Contains("exactly") || queryLower.Contains("verbatim"))
        {
            return QueryType.DeepRecall.ToString();
        }

        if (queryLower.Contains("write") || queryLower.Contains("create") || queryLower.Contains("implement"))
        {
            return QueryType.ProceduralTrigger.ToString();
        }

        return QueryType.Complex.ToString();
    }

    public async Task<ExtractedEntity[]> ExtractEntitiesAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        var entities = new List<ExtractedEntity>();

        // Simple entity extraction using keywords
        // In production, use Azure OpenAI for proper NER

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Extract potential technology mentions
        var techKeywords = new[] { "azure", "openai", "redis", "blob", ".net", "c#", "python", "api" };
        foreach (var word in words)
        {
            var lowerWord = word.ToLowerInvariant().Trim('.', ',', '!', '?');
            if (techKeywords.Contains(lowerWord))
            {
                entities.Add(new ExtractedEntity
                {
                    Key = "Technology",
                    Value = word,
                    Type = EntityType.Technology,
                    Importance = 0.7,
                    IsNovel = true,
                    Embedding = await GetEmbeddingAsync(word, cancellationToken)
                });
            }
        }

        // Extract decisions
        if (text.Contains("will", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("decided", StringComparison.OrdinalIgnoreCase))
        {
            entities.Add(new ExtractedEntity
            {
                Key = "Decision",
                Value = text.Length > 50 ? text[..50] + "..." : text,
                Type = EntityType.Decision,
                Importance = 0.9,
                IsNovel = true,
                Embedding = await GetEmbeddingAsync(text, cancellationToken)
            });
        }

        _logger.LogDebug("Extracted {Count} entities from text", entities.Count);

        return entities.ToArray();
    }

    public async Task<string> CompleteAsync(
        string prompt,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken);

        // Mock response generation
        // In production, use actual Azure OpenAI completion

        var responses = new[]
        {
            "This is a mock response from the LLM. In production, this would be generated by Azure OpenAI based on the memory context provided.",
            "I understand your query. Based on the conversation history and extracted facts, here's my response.",
            "Let me help you with that. According to our previous discussions and the information I have...",
            "Based on the context provided, I can assist you with this request."
        };

        var response = responses[_random.Next(responses.Length)];

        _logger.LogInformation("Generated mock completion (prompt length: {Length})", prompt.Length);

        return response;
    }

    public async Task<string> AnswerWithContextAsync(
        string query,
        MemoryContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(250, cancellationToken);

        // Build a simple response incorporating context
        var response = $"Based on the conversation context with {context.WorkingMemory.Length} recent messages, " +
                      $"{context.Facts.Length} relevant facts, and {context.ArchivedMessages.Length} archived messages:\n\n";

        if (context.AppliedProcedure != null)
        {
            response += $"[Applying learned pattern: {context.AppliedProcedure.Name}]\n\n";
        }

        response += $"Mock answer to: {query}\n\n";
        response += "In production, this would be a comprehensive answer generated by Azure OpenAI using the assembled context.";

        _logger.LogInformation(
            "Generated contextual answer (context tokens: {Tokens})",
            context.TotalTokens);

        return response;
    }

    public async Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);

        // Simple sentiment analysis
        // In production, use Azure Cognitive Services or OpenAI

        var textLower = text.ToLowerInvariant();

        var positiveWords = new[] { "great", "excellent", "good", "happy", "love", "wonderful", "amazing", "thanks" };
        var negativeWords = new[] { "bad", "terrible", "awful", "hate", "sad", "angry", "disappointed", "problem" };

        var positiveCount = positiveWords.Count(word => textLower.Contains(word));
        var negativeCount = negativeWords.Count(word => textLower.Contains(word));

        var score = (positiveCount - negativeCount) / (double)Math.Max(1, positiveCount + negativeCount);
        score = Math.Clamp(score, -1.0, 1.0);

        var sentiment = score switch
        {
            > 0.3 => "Positive",
            < -0.3 => "Negative",
            _ => "Neutral"
        };

        _logger.LogDebug("Sentiment analysis: {Sentiment} ({Score:F2})", sentiment, score);

        return (score, sentiment);
    }
}
