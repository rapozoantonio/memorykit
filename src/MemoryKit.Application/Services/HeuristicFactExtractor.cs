using System.Text.RegularExpressions;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Application.Services;

/// <summary>
/// Lightweight, regex-based fact extractor for common conversational patterns.
/// Provides sub-5ms extraction without LLM calls.
/// Supports 6 entity types: Person, Technology, Preference, Decision, Goal, Constraint.
/// </summary>
public static class HeuristicFactExtractor
{
    private const int MaxMessageLength = 10_000;
    private const int RegexTimeoutMs = 100;

    private static readonly string[] StopWords =
    {
        "a", "an", "the", "and", "or", "but", "is", "are", "was", "were",
        "to", "in", "on", "at", "by", "for", "with", "from", "of", "this"
    };

    #region Regex Patterns

    /// <summary>
    /// Matches person-related patterns like "My name is X", "I work as X", "I am a X".
    /// Pattern designed to avoid catastrophic backtracking with bounded quantifiers.
    /// </summary>
    private static readonly Regex PersonPattern = new(
        @"(?i)\b(?:my name is|i(?:'m| am)(?: a| an)?|i work as|i'm a|i'm an)\s+([a-zA-Z][\w\s-]{1,50}?)\b(?:\.|,|$|\s+(?:and|who|that|which))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches technology patterns like "using X", "built with X", "we use X", "we're using X".
    /// Captures framework/language/tool names with capital letters.
    /// </summary>
    private static readonly Regex TechnologyPattern = new(
        @"(?i)\b(?:using|built with|we(?:'re| are) using|we use|powered by|written in|running on)\s+([A-Z][\w\.\-\+\#]+(?:\s+(?!(?:for|to|with|and)\s)[A-Z\w\.\-\+\#]+)*)(?=\s*[\.\!,;]|$|\s+for\s|\s+to\s|\s+with\s|\s+and\s)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches preference patterns like "I prefer X", "I like X", "I favor X".
    /// </summary>
    private static readonly Regex PreferencePattern = new(
        @"(?i)\b(?:i prefer|i like|i favor|i enjoy|i love)\s+(?:to\s+)?([a-zA-Z][\w\s-]{1,50}?)\b(?:\.|,|$|\s+(?:over|than|because))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches decision patterns like "I decided to X", "We decided to X", "We will X", "Let's X".
    /// </summary>
    private static readonly Regex DecisionPattern = new(
        @"(?i)\b(?:(?:i|we)(?:'ve| have)? decided to|we(?:'ll| will)|let(?:'s| us)|we(?:'re| are) going to)\s+([a-zA-Z][\w\s-]{1,60}?)\b(?:\.|,|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches goal patterns like "I want to X", "planning to X", "goal is to X".
    /// </summary>
    private static readonly Regex GoalPattern = new(
        @"(?i)\b(?:i want to|planning to|my goal is to|we plan to|aiming to|hoping to)\s+([a-zA-Z][\w\s-]{1,60}?)\b(?:\.|,|$|\s+(?:by|before))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches constraint patterns like "must X", "we must X", "cannot X", "need to X".
    /// </summary>
    private static readonly Regex ConstraintPattern = new(
        @"(?i)\b(?:(?:we |i )?(?:must|cannot|can't|need to|have to|required to|should not|shouldn't))\s+([a-zA-Z][\w\s-]{1,50}?)\b(?:\.|,|$|\s+(?:because|due))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches temporal patterns like "7 years old", "age 25", "when I was 30".
    /// Extracts biographical age references from narratives.
    /// </summary>
    private static readonly Regex TemporalPattern = new(
        @"(?i)\b(?:(?:i was|at age|age)\s+)?(\d+)\s+years?\s+old|(?:age|aged)\s+(\d+)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches location patterns like "in Niterói", "at Barcelona", "from Paris".
    /// Captures capitalized proper nouns preceded by location prepositions.
    /// </summary>
    private static readonly Regex LocationPattern = new(
        @"(?i)\b(?:in|at|from|to)\s+([A-Z][a-zA-Z]{2,}(?:\s+[A-Z][a-zA-Z]+)?)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    /// <summary>
    /// Matches experience patterns like "first time riding", "learned to code", "remember visiting".
    /// Captures life events and biographical experiences.
    /// </summary>
    private static readonly Regex ExperiencePattern = new(
        @"(?i)\b(?:first time|learned to|remember|started)\s+([a-zA-Z][\w\s]{3,35}?)\b(?=[\.,!]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(RegexTimeoutMs)
    );

    #endregion

    /// <summary>
    /// Extracts semantic facts from text using regex heuristics.
    /// Returns List (not array) to differentiate from LLM service which returns array.
    /// Performance target: less than 5ms for messages up to 10KB.
    /// </summary>
    /// <param name="text">The message content to extract facts from.</param>
    /// <param name="useNarrativeFallback">If true, extract narrative fragments when structured extraction yields zero facts.</param>
    /// <param name="narrativeImportance">Importance score for narrative fragments (default 0.50).</param>
    /// <param name="maxFragments">Maximum number of narrative fragments to extract (default 3).</param>
    /// <returns>List of extracted entities (empty list if no matches, never null).</returns>
    public static List<ExtractedEntity> Extract(
        string text,
        bool useNarrativeFallback = true,
        double narrativeImportance = 0.50,
        int maxFragments = 3)
    {
        // Defensive coding: zero exceptions allowed
        if (string.IsNullOrWhiteSpace(text))
            return new List<ExtractedEntity>();

        // Truncate very long messages to cap worst-case performance
        if (text.Length > MaxMessageLength)
        {
            text = text.Substring(0, MaxMessageLength);
        }

        var entities = new List<ExtractedEntity>();

        try
        {
            // Extract each type sequentially (not parallel - more predictable performance)
            entities.AddRange(ExtractPersonEntities(text));
            entities.AddRange(ExtractTechnologyEntities(text));
            entities.AddRange(ExtractPreferenceEntities(text));
            entities.AddRange(ExtractDecisionEntities(text));
            entities.AddRange(ExtractGoalEntities(text));
            entities.AddRange(ExtractConstraintEntities(text));

            // Deduplicate by Key+Value (case-insensitive)
            var deduplicated = DeduplicateEntities(entities);

            // If zero structured facts found, try narrative extraction as fallback
            if (deduplicated.Count == 0 && useNarrativeFallback)
            {
                return ExtractNarrativeFragments(text, narrativeImportance, maxFragments);
            }

            return deduplicated;
        }
        catch (RegexMatchTimeoutException)
        {
            // Regex timeout - return what we have so far
            return new List<ExtractedEntity>();
        }
        catch (Exception)
        {
            // Ultimate fallback - never crash
            return new List<ExtractedEntity>();
        }
    }

    #region Private Extraction Methods

    private static List<ExtractedEntity> ExtractPersonEntities(string text)
    {
        var results = new List<ExtractedEntity>();
        var matches = PersonPattern.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var value = CleanEntityValue(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    results.Add(CreateEntity(
                        key: "Person",
                        value: value,
                        type: EntityType.Person,
                        importance: 0.75
                    ));
                }
            }
        }

        return results;
    }

    private static List<ExtractedEntity> ExtractTechnologyEntities(string text)
    {
        var results = new List<ExtractedEntity>();
        var matches = TechnologyPattern.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var value = CleanEntityValue(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    results.Add(CreateEntity(
                        key: "Technology",
                        value: value,
                        type: EntityType.Technology,
                        importance: 0.70
                    ));
                }
            }
        }

        return results;
    }

    private static List<ExtractedEntity> ExtractPreferenceEntities(string text)
    {
        var results = new List<ExtractedEntity>();
        var matches = PreferencePattern.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var value = CleanEntityValue(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    results.Add(CreateEntity(
                        key: "Preference",
                        value: value,
                        type: EntityType.Preference,
                        importance: 0.60
                    ));
                }
            }
        }

        return results;
    }

    private static List<ExtractedEntity> ExtractDecisionEntities(string text)
    {
        var results = new List<ExtractedEntity>();
        var matches = DecisionPattern.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var value = CleanEntityValue(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    results.Add(CreateEntity(
                        key: "Decision",
                        value: value,
                        type: EntityType.Decision,
                        importance: 0.85
                    ));
                }
            }
        }

        return results;
    }

    private static List<ExtractedEntity> ExtractGoalEntities(string text)
    {
        var results = new List<ExtractedEntity>();
        var matches = GoalPattern.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var value = CleanEntityValue(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    results.Add(CreateEntity(
                        key: "Goal",
                        value: value,
                        type: EntityType.Goal,
                        importance: 0.80
                    ));
                }
            }
        }

        return results;
    }

    private static List<ExtractedEntity> ExtractConstraintEntities(string text)
    {
        var results = new List<ExtractedEntity>();
        var matches = ConstraintPattern.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var value = CleanEntityValue(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    results.Add(CreateEntity(
                        key: "Constraint",
                        value: value,
                        type: EntityType.Constraint,
                        importance: 0.75
                    ));
                }
            }
        }

        return results;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Cleans entity value by removing leading/trailing stop words and whitespace.
    /// Only trims leading/trailing stops, preserves internal stop words.
    /// </summary>
    private static string CleanEntityValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();

        // Remove leading stop words (max 2 iterations to prevent over-trimming)
        for (int i = 0; i < 2; i++)
        {
            var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1 && StopWords.Contains(words[0].ToLowerInvariant()))
            {
                value = string.Join(' ', words.Skip(1));
            }
            else
            {
                break;
            }
        }

        // Remove trailing stop words (max 2 iterations)
        for (int i = 0; i < 2; i++)
        {
            var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1 && StopWords.Contains(words[^1].ToLowerInvariant()))
            {
                value = string.Join(' ', words.SkipLast(1));
            }
            else
            {
                break;
            }
        }

        return value.Trim();
    }

    /// <summary>
    /// Creates an ExtractedEntity with deterministic embedding.
    /// Embedding generation matches SemanticKernelService.GenerateFallbackEmbedding() behavior.
    /// </summary>
    private static ExtractedEntity CreateEntity(string key, string value, EntityType type, double importance)
    {
        var embedding = GenerateDeterministicEmbedding($"{key}:{value}");

        return new ExtractedEntity
        {
            Key = key,
            Value = value,
            Type = type,
            Importance = importance,
            IsNovel = true, // Heuristics cannot determine novelty
            Embedding = embedding
        };
    }

    /// <summary>
    /// Generates a deterministic 384-dimension embedding based on text hash.
    /// Matches SemanticKernelService.GenerateFallbackEmbedding() behavior.
    /// NOTE: This is duplicated code for performance (keeps HeuristicFactExtractor static/dependency-free).
    /// </summary>
    private static float[] GenerateDeterministicEmbedding(string text)
    {
        var embedding = new float[384];
        var hash = text.GetHashCode();
        var random = new Random(hash);

        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }

        // Normalize to unit vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(embedding[i] / magnitude);
        }

        return embedding;
    }

    /// <summary>
    /// Deduplicates entities by Key+Value (case-insensitive).
    /// Keeps the first occurrence of each unique entity.
    /// </summary>
    private static List<ExtractedEntity> DeduplicateEntities(List<ExtractedEntity> entities)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduplicated = new List<ExtractedEntity>();

        foreach (var entity in entities)
        {
            var signature = $"{entity.Key}|{entity.Value}";
            if (seen.Add(signature))
            {
                deduplicated.Add(entity);
            }
        }

        return deduplicated;
    }

    /// <summary>
    /// Chunks text into sentences for narrative extraction.
    /// Filters out very short (<3 words) and very long (>20 words) sentences.
    /// </summary>
    private static List<string> ChunkIntoSentences(string text)
    {
        var sentences = Regex.Split(text, @"[.!?]+")
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(words => words.Length >= 3 && words.Length <= 20)
            .Select(words => string.Join(' ', words))
            .ToList();

        return sentences;
    }

    /// <summary>
    /// Extracts temporal entities (age references) from text.
    /// Adds results to the provided list.
    /// </summary>
    private static void ExtractTemporalEntities(string text, List<ExtractedEntity> results)
    {
        try
        {
            var matches = TemporalPattern.Matches(text);
            foreach (Match match in matches)
            {
                var age = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                if (!string.IsNullOrWhiteSpace(age))
                {
                    results.Add(CreateEntity(
                        key: "Age",
                        value: $"{age} years old",
                        type: EntityType.Other,
                        importance: 0.50
                    ));
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout - skip this pattern
        }
    }

    /// <summary>
    /// Extracts location entities (places) from text.
    /// Adds results to the provided list.
    /// </summary>
    private static void ExtractLocationEntities(string text, List<ExtractedEntity> results)
    {
        try
        {
            var matches = LocationPattern.Matches(text);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    var place = CleanEntityValue(match.Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(place) && place.Length >= 2)
                    {
                        results.Add(CreateEntity(
                            key: "Location",
                            value: place,
                            type: EntityType.Place,
                            importance: 0.55
                        ));
                    }
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout - skip this pattern
        }
    }

    /// <summary>
    /// Extracts experience entities (life events) from text.
    /// Adds results to the provided list.
    /// </summary>
    private static void ExtractExperienceEntities(string text, List<ExtractedEntity> results)
    {
        try
        {
            var matches = ExperiencePattern.Matches(text);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    var experience = CleanEntityValue(match.Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(experience) && experience.Length >= 5)
                    {
                        results.Add(CreateEntity(
                            key: "Experience",
                            value: experience,
                            type: EntityType.Other,
                            importance: 0.50
                        ));
                    }
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout - skip this pattern
        }
    }

    /// <summary>
    /// Extracts narrative fragments from text when structured extraction yields zero facts.
    /// Provides graceful degradation for biographical/narrative content.
    /// </summary>
    private static List<ExtractedEntity> ExtractNarrativeFragments(
        string text,
        double importanceScore,
        int maxFragments)
    {
        var entities = new List<ExtractedEntity>();
        var sentences = ChunkIntoSentences(text);

        // Process each sentence (up to maxFragments)
        foreach (var sentence in sentences.Take(maxFragments))
        {
            // Extract biographical keywords from sentence
            ExtractTemporalEntities(sentence, entities);
            ExtractLocationEntities(sentence, entities);
            ExtractExperienceEntities(sentence, entities);

            // Store full cleaned sentence as narrative memory
            var cleaned = CleanEntityValue(sentence);
            if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Length >= 10)
            {
                entities.Add(CreateEntity(
                    key: "Memory",
                    value: cleaned,
                    type: EntityType.Other,
                    importance: importanceScore
                ));
            }
        }

        // Deduplicate within message (e.g., "Barcelona" mentioned twice)
        return DeduplicateEntities(entities);
    }

    #endregion
}
