using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Infrastructure.SemanticKernel;

/// <summary>
/// Production implementation of Semantic Kernel Service using Azure OpenAI.
/// </summary>
public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService? _embeddings;
    private readonly IChatCompletionService? _chat;
    private readonly ILogger<SemanticKernelService> _logger;
    private readonly bool _isConfigured;

    public SemanticKernelService(
        IConfiguration configuration,
        ILogger<SemanticKernelService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var builder = Kernel.CreateBuilder();

            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var apiKey = configuration["AzureOpenAI:ApiKey"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"];
            var embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"];

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                // Add Azure OpenAI Chat Completion
                if (!string.IsNullOrEmpty(deploymentName))
                {
                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName,
                        endpoint,
                        apiKey);
                    _logger.LogInformation("Azure OpenAI Chat Completion configured with deployment: {Deployment}", deploymentName);
                }

                // Add Azure OpenAI Text Embedding
                if (!string.IsNullOrEmpty(embeddingDeployment))
                {
                    builder.AddAzureOpenAITextEmbeddingGeneration(
                        embeddingDeployment,
                        endpoint,
                        apiKey);
                    _logger.LogInformation("Azure OpenAI Embeddings configured with deployment: {Deployment}", embeddingDeployment);
                }

                _kernel = builder.Build();
                _embeddings = _kernel.GetService<ITextEmbeddingGenerationService>();
                _chat = _kernel.GetService<IChatCompletionService>();
                _isConfigured = true;

                _logger.LogInformation("SemanticKernelService initialized successfully");
            }
            else
            {
                _kernel = Kernel.CreateBuilder().Build();
                _isConfigured = false;
                _logger.LogWarning("Azure OpenAI not configured. Service will use fallback implementations.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SemanticKernelService. Using fallback mode.");
            _kernel = Kernel.CreateBuilder().Build();
            _isConfigured = false;
        }
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _embeddings == null)
        {
            _logger.LogDebug("Embeddings not configured, using fallback");
            return GenerateFallbackEmbedding(text);
        }

        try
        {
            var embedding = await _embeddings.GenerateEmbeddingAsync(
                text,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Generated embedding of dimension {Dimension}", embedding.Length);
            return embedding.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding, using fallback");
            return GenerateFallbackEmbedding(text);
        }
    }

    public async Task<string> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _chat == null)
        {
            _logger.LogDebug("Chat not configured, using fallback classification");
            return FallbackClassifyQuery(query);
        }

        try
        {
            var prompt = $@"Classify this query into one of these types:
- Continuation: User wants to continue previous topic
- FactRetrieval: User asking for specific information
- DeepRecall: User wants exact quotes or detailed history
- Complex: Multi-faceted question requiring deep analysis
- ProceduralTrigger: Task that matches a learned routine

Query: {query}

Respond with ONLY the classification type (one word).";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a query classification expert.");
            chatHistory.AddUserMessage(prompt);

            var response = await _chat.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            var classification = response.Content?.Trim() ?? "Complex";
            _logger.LogDebug("Classified query as: {Classification}", classification);

            return classification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to classify query, using fallback");
            return FallbackClassifyQuery(query);
        }
    }

    public async Task<ExtractedEntity[]> ExtractEntitiesAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _chat == null)
        {
            _logger.LogDebug("Chat not configured, using fallback entity extraction");
            return FallbackExtractEntities(text);
        }

        try
        {
            var prompt = $@"Extract key entities from this text. Return a JSON array.

Text: {text}

Format:
[
  {{
    ""key"": ""EntityName"",
    ""value"": ""EntityValue"",
    ""type"": ""Person|Place|Technology|Decision|Preference|Constraint|Goal|Other"",
    ""importance"": 0.0-1.0
  }}
]

Return ONLY valid JSON array, no additional text.";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are an entity extraction expert. Always return valid JSON.");
            chatHistory.AddUserMessage(prompt);

            var response = await _chat.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            var jsonContent = response.Content?.Trim() ?? "[]";

            // Clean up markdown code blocks if present
            if (jsonContent.StartsWith("```"))
            {
                var lines = jsonContent.Split('\n');
                jsonContent = string.Join('\n', lines.Skip(1).SkipLast(1));
            }

            var entities = JsonSerializer.Deserialize<List<EntityDto>>(jsonContent) ?? new List<EntityDto>();

            var result = new List<ExtractedEntity>();
            foreach (var entity in entities)
            {
                var embedding = await GetEmbeddingAsync($"{entity.Key}: {entity.Value}", cancellationToken);

                result.Add(new ExtractedEntity
                {
                    Key = entity.Key,
                    Value = entity.Value,
                    Type = Enum.TryParse<EntityType>(entity.Type, true, out var type) ? type : EntityType.Other,
                    Importance = entity.Importance,
                    IsNovel = true,
                    Embedding = embedding
                });
            }

            _logger.LogInformation("Extracted {Count} entities from text", result.Count);
            return result.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract entities, using fallback");
            return FallbackExtractEntities(text);
        }
    }

    public async Task<string> CompleteAsync(
        string prompt,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _chat == null)
        {
            _logger.LogDebug("Chat not configured, using fallback completion");
            return $"[Fallback Response]\n{prompt.Substring(0, Math.Min(200, prompt.Length))}...";
        }

        try
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var response = await _chat.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            return response.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete prompt");
            return $"Error generating response: {ex.Message}";
        }
    }

    public async Task<string> AnswerWithContextAsync(
        string query,
        MemoryContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _chat == null)
        {
            _logger.LogDebug("Chat not configured, using fallback answer generation");
            return GenerateFallbackAnswer(query, context);
        }

        try
        {
            var systemPrompt = context.ToPromptContext();

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(query);

            var response = await _chat.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Generated answer for query using {TokenCount} tokens of context",
                context.TotalTokens);

            return response.Content ?? "I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer with context");
            return GenerateFallbackAnswer(query, context);
        }
    }

    public async Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _chat == null)
        {
            _logger.LogDebug("Chat not configured, using fallback sentiment analysis");
            return FallbackAnalyzeSentiment(text);
        }

        try
        {
            var prompt = $@"Analyze the sentiment of this text.

Text: {text}

Return ONLY a JSON object in this exact format:
{{
  ""score"": -1.0 to 1.0 (negative to positive),
  ""sentiment"": ""Positive|Negative|Neutral""
}}";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a sentiment analysis expert. Always return valid JSON.");
            chatHistory.AddUserMessage(prompt);

            var response = await _chat.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            var jsonContent = response.Content?.Trim() ?? "{}";

            // Clean up markdown code blocks if present
            if (jsonContent.StartsWith("```"))
            {
                var lines = jsonContent.Split('\n');
                jsonContent = string.Join('\n', lines.Skip(1).SkipLast(1));
            }

            var sentimentResult = JsonSerializer.Deserialize<SentimentDto>(jsonContent);
            if (sentimentResult != null)
            {
                return (sentimentResult.Score, sentimentResult.Sentiment);
            }

            return (0.0, "Neutral");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze sentiment, using fallback");
            return FallbackAnalyzeSentiment(text);
        }
    }

    // Fallback methods for when Azure OpenAI is not configured

    private float[] GenerateFallbackEmbedding(string text)
    {
        // Generate a simple hash-based embedding (dimension 384 to match common models)
        var embedding = new float[384];
        var hash = text.GetHashCode();
        var random = new Random(hash);

        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Range: -1 to 1
        }

        // Normalize
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(embedding[i] / magnitude);
        }

        return embedding;
    }

    private string FallbackClassifyQuery(string query)
    {
        var lower = query.ToLowerInvariant();

        if (lower.StartsWith("continue") || lower.StartsWith("go on"))
            return "Continuation";

        if (lower.Contains("what was") || lower.Contains("what is") || lower.Contains("who is"))
            return "FactRetrieval";

        if (lower.Contains("quote") || lower.Contains("exactly") || lower.Contains("verbatim"))
            return "DeepRecall";

        if (lower.Contains("write") || lower.Contains("create") || lower.Contains("generate"))
            return "ProceduralTrigger";

        return "Complex";
    }

    private ExtractedEntity[] FallbackExtractEntities(string text)
    {
        var entities = new List<ExtractedEntity>();

        // Extract code blocks
        if (text.Contains("```"))
        {
            entities.Add(new ExtractedEntity
            {
                Key = "Contains Code",
                Value = "true",
                Type = EntityType.Technology,
                Importance = 0.8,
                IsNovel = true,
                Embedding = GenerateFallbackEmbedding("code")
            });
        }

        // Extract URLs
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var urls = words.Where(w => w.StartsWith("http://") || w.StartsWith("https://")).Take(3);
        foreach (var url in urls)
        {
            entities.Add(new ExtractedEntity
            {
                Key = "Referenced URL",
                Value = url,
                Type = EntityType.Other,
                Importance = 0.6,
                IsNovel = true,
                Embedding = GenerateFallbackEmbedding(url)
            });
        }

        return entities.ToArray();
    }

    private string GenerateFallbackAnswer(string query, MemoryContext context)
    {
        var response = new System.Text.StringBuilder();

        response.AppendLine($"[Fallback Response - Azure OpenAI not configured]");
        response.AppendLine();
        response.AppendLine($"Question: {query}");
        response.AppendLine();

        if (context.AppliedProcedure != null)
        {
            response.AppendLine($"Applied Procedure: {context.AppliedProcedure.Name}");
            response.AppendLine();
        }

        response.AppendLine("Retrieved Context:");
        response.AppendLine($"- Recent messages: {context.WorkingMemory.Length}");
        response.AppendLine($"- Facts: {context.Facts.Length}");
        response.AppendLine($"- Archived messages: {context.ArchivedMessages.Length}");
        response.AppendLine($"- Total tokens: {context.TotalTokens}");

        if (context.Facts.Any())
        {
            response.AppendLine();
            response.AppendLine("Relevant Facts:");
            foreach (var fact in context.Facts.Take(5))
            {
                response.AppendLine($"  • {fact.Key}: {fact.Value}");
            }
        }

        return response.ToString();
    }

    private (double Score, string Sentiment) FallbackAnalyzeSentiment(string text)
    {
        var lower = text.ToLowerInvariant();
        double score = 0.0;

        // Positive markers
        var positiveWords = new[] { "great", "excellent", "good", "happy", "love", "perfect", "amazing", "wonderful" };
        score += positiveWords.Count(w => lower.Contains(w)) * 0.2;

        // Negative markers
        var negativeWords = new[] { "bad", "terrible", "hate", "awful", "problem", "issue", "error", "fail" };
        score -= negativeWords.Count(w => lower.Contains(w)) * 0.2;

        score = Math.Clamp(score, -1.0, 1.0);

        var sentiment = score > 0.1 ? "Positive" : score < -0.1 ? "Negative" : "Neutral";
        return (score, sentiment);
    }

    // DTOs for JSON deserialization
    private class EntityDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = "Other";
        public double Importance { get; set; } = 0.5;
    }

    private class SentimentDto
    {
        public double Score { get; set; }
        public string Sentiment { get; set; } = "Neutral";
    }
}
