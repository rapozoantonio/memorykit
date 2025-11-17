using System.Collections.Concurrent;
using System.Text.Json;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.Azure;
using MemoryKit.Infrastructure.SemanticKernel;

namespace MemoryKit.Infrastructure.InMemory;

/// <summary>
/// Enhanced in-memory implementation of Procedural Memory Service with AI-powered pattern learning.
/// Stores learned patterns and routines with advanced reinforcement learning.
/// </summary>
public class EnhancedInMemoryProceduralMemoryService : IProceduralMemoryService
{
    private readonly ConcurrentDictionary<string, List<ProceduralPattern>> _patterns = new();
    private readonly ISemanticKernelService? _semanticKernel;
    private readonly ILogger<EnhancedInMemoryProceduralMemoryService> _logger;
    private readonly object _lock = new();

    public EnhancedInMemoryProceduralMemoryService(
        ILogger<EnhancedInMemoryProceduralMemoryService> logger,
        ISemanticKernelService? semanticKernel = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _semanticKernel = semanticKernel;
    }

    public async Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (!_patterns.TryGetValue(userId, out var userPatterns))
            return null;

        _logger.LogDebug("Matching query against {Count} patterns for user {UserId}",
            userPatterns.Count, userId);

        lock (_lock)
        {
            var queryLower = query.ToLowerInvariant();
            ProceduralPattern? bestMatch = null;
            double bestScore = 0;

            foreach (var pattern in userPatterns.OrderByDescending(p => p.UsageCount))
            {
                foreach (var trigger in pattern.Triggers)
                {
                    double score = 0;

                    switch (trigger.Type)
                    {
                        case TriggerType.Keyword:
                            if (queryLower.Contains(trigger.Pattern.ToLowerInvariant()))
                            {
                                score = 0.9;
                            }
                            break;

                        case TriggerType.Regex:
                            if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, trigger.Pattern))
                            {
                                score = 0.85;
                            }
                            break;

                        case TriggerType.Semantic:
                            // Use embedding similarity if available
                            if (_semanticKernel != null && trigger.Embedding?.Length > 0)
                            {
                                try
                                {
                                    var queryEmbedding = _semanticKernel.GetEmbeddingAsync(query, cancellationToken).Result;
                                    score = CalculateCosineSimilarity(queryEmbedding, trigger.Embedding);
                                }
                                catch
                                {
                                    // Fallback to keyword matching
                                    if (queryLower.Contains(trigger.Pattern.ToLowerInvariant()))
                                    {
                                        score = 0.7;
                                    }
                                }
                            }
                            else
                            {
                                // Fallback to keyword matching
                                if (queryLower.Contains(trigger.Pattern.ToLowerInvariant()))
                                {
                                    score = 0.7;
                                }
                            }
                            break;
                    }

                    // Apply confidence threshold and usage boost
                    if (score > pattern.ConfidenceThreshold)
                    {
                        // Boost score based on usage count (reinforcement learning)
                        var usageBoost = Math.Min(pattern.UsageCount * 0.01, 0.15);
                        score += usageBoost;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMatch = pattern;
                        }
                    }
                }
            }

            if (bestMatch != null)
            {
                bestMatch.RecordUsage();
                _logger.LogInformation(
                    "Matched pattern '{PatternName}' with score {Score:F3} (used {UsageCount} times)",
                    bestMatch.Name,
                    bestScore,
                    bestMatch.UsageCount);

                // Trigger pattern consolidation if needed
                _ = Task.Run(() => ConsolidatePatternsAsync(userId, cancellationToken), cancellationToken);
            }

            return bestMatch;
        }
    }

    public Task StorePatternAsync(
        ProceduralPattern pattern,
        CancellationToken cancellationToken = default)
    {
        var userPatterns = _patterns.GetOrAdd(pattern.UserId, _ => new List<ProceduralPattern>());

        lock (_lock)
        {
            // Check if similar pattern already exists
            var existingPattern = userPatterns.FirstOrDefault(p =>
                IsSimilarPattern(p, pattern));

            if (existingPattern != null)
            {
                _logger.LogDebug("Merging with existing pattern: {PatternName}", existingPattern.Name);
                MergePatterns(existingPattern, pattern);
            }
            else
            {
                userPatterns.Add(pattern);
                _logger.LogInformation("Stored new procedural pattern: {PatternName}", pattern.Name);
            }

            // Enforce max patterns limit
            if (userPatterns.Count > 100)
            {
                var toRemove = userPatterns
                    .OrderBy(p => p.UsageCount)
                    .ThenBy(p => p.LastUsed)
                    .First();
                userPatterns.Remove(toRemove);
                _logger.LogDebug("Removed least used pattern: {PatternName}", toRemove.Name);
            }
        }

        return Task.CompletedTask;
    }

    public async Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        // Use AI-powered pattern detection if available
        if (_semanticKernel != null)
        {
            try
            {
                var patterns = await DetectPatternsWithAIAsync(userId, message, cancellationToken);
                foreach (var pattern in patterns)
                {
                    await StorePatternAsync(pattern, cancellationToken);
                }
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI pattern detection failed, using fallback");
            }
        }

        // Fallback to rule-based detection
        await DetectPatternsWithRulesAsync(userId, message, cancellationToken);
    }

    public Task<ProceduralPattern[]> GetUserPatternsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!_patterns.TryGetValue(userId, out var userPatterns))
            return Task.FromResult(Array.Empty<ProceduralPattern>());

        lock (_lock)
        {
            return Task.FromResult(userPatterns
                .OrderByDescending(p => p.UsageCount)
                .ThenByDescending(p => p.LastUsed)
                .ToArray());
        }
    }

    /// <summary>
    /// Uses AI to detect procedural patterns from messages.
    /// </summary>
    private async Task<ProceduralPattern[]> DetectPatternsWithAIAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken)
    {
        if (_semanticKernel == null)
            return Array.Empty<ProceduralPattern>();

        var prompt = $@"Analyze this message for procedural instructions or patterns that should be remembered for future interactions.

Message: {message.Content}

Identify any:
1. Explicit preferences (""I prefer..."", ""Always..."", ""Never..."")
2. Workflow patterns (""When X, then Y..."")
3. Format preferences (""Format code as..."", ""Structure as..."")
4. Decision rules (""If X, do Y..."")
5. Constraints (""Don't include..."", ""Make sure to..."")

Return a JSON array of patterns found:
[
  {{
    ""name"": ""Brief pattern name"",
    ""description"": ""What this pattern does"",
    ""instructionTemplate"": ""How to apply this pattern"",
    ""triggerKeywords"": [""keyword1"", ""keyword2""],
    ""confidence"": 0.0-1.0
  }}
]

Return ONLY valid JSON array. If no patterns found, return empty array [].";

        var response = await _semanticKernel.CompleteAsync(prompt, 1000, cancellationToken);

        try
        {
            // Clean up markdown code blocks if present
            var jsonContent = response.Trim();
            if (jsonContent.StartsWith("```"))
            {
                var lines = jsonContent.Split('\n');
                jsonContent = string.Join('\n', lines.Skip(1).SkipLast(1));
                jsonContent = jsonContent.Replace("```json", "").Trim();
            }

            var detectedPatterns = JsonSerializer.Deserialize<List<PatternDetectionDto>>(jsonContent)
                ?? new List<PatternDetectionDto>();

            var patterns = new List<ProceduralPattern>();
            foreach (var detected in detectedPatterns)
            {
                if (detected.Confidence < 0.6)
                    continue;

                var triggers = new List<PatternTrigger>();

                // Add keyword triggers
                foreach (var keyword in detected.TriggerKeywords ?? Array.Empty<string>())
                {
                    triggers.Add(new PatternTrigger
                    {
                        Type = TriggerType.Keyword,
                        Pattern = keyword.ToLowerInvariant()
                    });
                }

                // Add semantic trigger with embedding
                var embedding = await _semanticKernel.GetEmbeddingAsync(
                    detected.InstructionTemplate,
                    cancellationToken);

                triggers.Add(new PatternTrigger
                {
                    Type = TriggerType.Semantic,
                    Pattern = detected.Description,
                    Embedding = embedding
                });

                var pattern = ProceduralPattern.Create(
                    userId,
                    detected.Name,
                    detected.Description,
                    triggers.ToArray(),
                    detected.InstructionTemplate,
                    detected.Confidence);

                patterns.Add(pattern);
            }

            _logger.LogInformation("Detected {Count} patterns using AI", patterns.Count);
            return patterns.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI pattern detection response");
            return Array.Empty<ProceduralPattern>();
        }
    }

    /// <summary>
    /// Uses rule-based heuristics to detect patterns (fallback).
    /// </summary>
    private async Task DetectPatternsWithRulesAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken)
    {
        var content = message.Content.ToLowerInvariant();

        // Detect explicit procedural instructions
        var proceduralMarkers = new[]
        {
            ("always", "Always Instruction"),
            ("never", "Never Instruction"),
            ("from now on", "Future Instruction"),
            ("remember to", "Reminder Instruction"),
            ("make sure to", "Ensure Instruction"),
            ("i prefer", "User Preference"),
            ("format", "Format Preference")
        };

        foreach (var (marker, patternType) in proceduralMarkers)
        {
            if (content.Contains(marker))
            {
                var pattern = ProceduralPattern.Create(
                    userId,
                    $"{patternType}: {ExtractKeyPhrase(content, marker)}",
                    message.Content,
                    new[]
                    {
                        new PatternTrigger
                        {
                            Type = TriggerType.Keyword,
                            Pattern = ExtractKeyword(content)
                        }
                    },
                    message.Content,
                    confidenceThreshold: 0.7);

                await StorePatternAsync(pattern, cancellationToken);
                _logger.LogDebug("Detected rule-based pattern: {PatternName}", pattern.Name);
            }
        }
    }

    /// <summary>
    /// Consolidates similar patterns to reduce redundancy.
    /// </summary>
    private async Task ConsolidatePatternsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (!_patterns.TryGetValue(userId, out var userPatterns))
            return;

        await Task.Delay(100, cancellationToken); // Debounce

        lock (_lock)
        {
            var toConsolidate = new List<(ProceduralPattern, ProceduralPattern)>();

            for (int i = 0; i < userPatterns.Count; i++)
            {
                for (int j = i + 1; j < userPatterns.Count; j++)
                {
                    if (IsSimilarPattern(userPatterns[i], userPatterns[j]))
                    {
                        toConsolidate.Add((userPatterns[i], userPatterns[j]));
                    }
                }
            }

            foreach (var (pattern1, pattern2) in toConsolidate)
            {
                // Keep the more-used pattern, merge the other
                if (pattern1.UsageCount >= pattern2.UsageCount)
                {
                    MergePatterns(pattern1, pattern2);
                    userPatterns.Remove(pattern2);
                    _logger.LogDebug("Consolidated pattern '{P2}' into '{P1}'", pattern2.Name, pattern1.Name);
                }
                else
                {
                    MergePatterns(pattern2, pattern1);
                    userPatterns.Remove(pattern1);
                    _logger.LogDebug("Consolidated pattern '{P1}' into '{P2}'", pattern1.Name, pattern2.Name);
                }
            }

            if (toConsolidate.Any())
            {
                _logger.LogInformation("Consolidated {Count} similar patterns for user {UserId}",
                    toConsolidate.Count, userId);
            }
        }
    }

    /// <summary>
    /// Determines if two patterns are similar enough to be merged.
    /// </summary>
    private bool IsSimilarPattern(ProceduralPattern p1, ProceduralPattern p2)
    {
        // Check name similarity
        var nameSimilarity = CalculateStringSimilarity(
            p1.Name.ToLowerInvariant(),
            p2.Name.ToLowerInvariant());

        if (nameSimilarity > 0.8)
            return true;

        // Check trigger overlap
        var triggers1 = p1.Triggers.Select(t => t.Pattern.ToLowerInvariant()).ToHashSet();
        var triggers2 = p2.Triggers.Select(t => t.Pattern.ToLowerInvariant()).ToHashSet();
        var overlap = triggers1.Intersect(triggers2).Count();
        var total = triggers1.Union(triggers2).Count();

        if (total > 0 && (double)overlap / total > 0.6)
            return true;

        return false;
    }

    /// <summary>
    /// Merges pattern2 into pattern1.
    /// </summary>
    private void MergePatterns(ProceduralPattern target, ProceduralPattern source)
    {
        // Merge triggers (avoid duplicates)
        var existingTriggers = target.Triggers.Select(t => t.Pattern.ToLowerInvariant()).ToHashSet();
        var newTriggers = source.Triggers
            .Where(t => !existingTriggers.Contains(t.Pattern.ToLowerInvariant()))
            .ToList();

        if (newTriggers.Any())
        {
            target.SetTriggers(target.Triggers.Concat(newTriggers).ToArray());
        }

        // Record usage from merged pattern
        for (int i = 0; i < source.UsageCount; i++)
        {
            target.RecordUsage();
        }
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    private double CalculateCosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0;

        var dotProduct = a.Zip(b, (x, y) => x * y).Sum();
        var magnitudeA = Math.Sqrt(a.Sum(x => x * x));
        var magnitudeB = Math.Sqrt(b.Sum(x => x * x));

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Calculates string similarity using Levenshtein distance.
    /// </summary>
    private double CalculateStringSimilarity(string s1, string s2)
    {
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++)
            d[i, 0] = i;
        for (int j = 0; j <= n; j++)
            d[0, j] = j;

        for (int j = 1; j <= n; j++)
        {
            for (int i = 1; i <= m; i++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[m, n];
    }

    /// <summary>
    /// Extracts a key phrase near a marker.
    /// </summary>
    private string ExtractKeyPhrase(string content, string marker)
    {
        var index = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
            return "pattern";

        var startIndex = Math.Max(0, index);
        var length = Math.Min(50, content.Length - startIndex);
        var phrase = content.Substring(startIndex, length).Trim();

        // Take first sentence
        var sentenceEnd = phrase.IndexOfAny(new[] { '.', '!', '?' });
        if (sentenceEnd > 0)
            phrase = phrase.Substring(0, sentenceEnd);

        return phrase.Length > 30 ? phrase.Substring(0, 30) + "..." : phrase;
    }

    /// <summary>
    /// Extracts a keyword from content.
    /// </summary>
    private string ExtractKeyword(string content)
    {
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.FirstOrDefault(w => w.Length > 4) ?? "general";
    }

    // DTO for AI pattern detection
    private class PatternDetectionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InstructionTemplate { get; set; } = string.Empty;
        public string[]? TriggerKeywords { get; set; }
        public double Confidence { get; set; } = 0.7;
    }
}
