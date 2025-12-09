using Microsoft.Extensions.Logging;
using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using Moq;
using Xunit;

namespace MemoryKit.Application.Tests.Services;

/// <summary>
/// Tests for smart heuristics-based query classification in PrefrontalController.
/// </summary>
public class PrefrontalControllerSmartHeuristicsTests
{
    private readonly PrefrontalController _controller;
    private readonly Mock<ILogger<PrefrontalController>> _mockLogger;

    public PrefrontalControllerSmartHeuristicsTests()
    {
        _mockLogger = new Mock<ILogger<PrefrontalController>>();
        _controller = new PrefrontalController(_mockLogger.Object);
    }

    [Theory]
    [InlineData("What database do we use?")]
    [InlineData("Tell me about our caching strategy")]
    [InlineData("Where is the configuration file?")]
    public async Task FactRetrieval_SimpleQuestions_ShouldClassifyAsFactRetrieval(string query)
    {
        // Arrange
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.Equal(QueryType.FactRetrieval, plan.Type);
        Assert.Contains(MemoryLayer.WorkingMemory, plan.LayersToUse);
        Assert.Contains(MemoryLayer.SemanticMemory, plan.LayersToUse);
    }

    [Theory]
    [InlineData("Should we use Redis or Memcached?")]
    [InlineData("I think we should migrate to PostgreSQL")]
    [InlineData("We need to decide on the architecture approach")]
    public async Task Complex_DecisionQueries_ShouldClassifyAsComplex(string query)
    {
        // Arrange
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.Equal(QueryType.Complex, plan.Type);
        // Complex queries should use all layers or multiple layers
        Assert.True(plan.LayersToUse.Count >= 2);
    }

    [Theory]
    [InlineData("How do we handle database migrations?")]
    [InlineData("How have we typically scaled our services?")]
    [InlineData("What's our approach to error handling?")]
    public async Task ProceduralTrigger_HowToQueries_ShouldClassifyAsProceduralTrigger(string query)
    {
        // Arrange
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.Equal(QueryType.ProceduralTrigger, plan.Type);
        Assert.Contains(MemoryLayer.ProceduralMemory, plan.LayersToUse);
    }

    [Theory]
    [InlineData("Tell me the story of our architecture evolution")]
    [InlineData("Explain what happened during the last deployment")]
    [InlineData("Walk me through the migration process we did")]
    public async Task DeepRecall_NarrativeQueries_ShouldClassifyAsDeepRecall(string query)
    {
        // Arrange
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.Equal(QueryType.DeepRecall, plan.Type);
        Assert.Contains(MemoryLayer.EpisodicMemory, plan.LayersToUse);
    }

    [Theory]
    [InlineData("continue")]
    [InlineData("go on")]
    [InlineData("and then")]
    public async Task Continuation_ContinuationPatterns_ShouldClassifyAsContinuation(string query)
    {
        // Arrange
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.Equal(QueryType.Continuation, plan.Type);
        Assert.Single(plan.LayersToUse);
        Assert.Contains(MemoryLayer.WorkingMemory, plan.LayersToUse);
    }

    [Fact]
    public async Task SignalBased_AmbiguousQuery_ShouldUseMultipleLayers()
    {
        // Arrange
        var query = "hmm, maybe we should think about that database thing?";
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert - Ambiguous queries should fall back to broader search
        Assert.True(plan.LayersToUse.Count > 1, "Ambiguous queries should use multiple layers");
    }

    [Theory]
    [InlineData("What should we NOT do when scaling?")]
    [InlineData("Which approach should we avoid?")]
    public async Task Negation_NegatedQuestions_ShouldBoostRetrievalSignal(string query)
    {
        // Arrange
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        // Negated questions asking about what NOT to do are typically fact retrieval
        Assert.True(
            plan.Type == QueryType.FactRetrieval || plan.LayersToUse.Contains(MemoryLayer.SemanticMemory),
            "Negated questions should retrieve factual information");
    }

    [Theory]
    [InlineData("SHOULD WE DO THIS NOW???")]
    [InlineData("This is CRITICAL!!")]
    public async Task Intensity_HighIntensityLanguage_ShouldProcessSuccessfully(string query)
    {
        // Arrange
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan.LayersToUse);
        // Intensity should influence classification but not break it
    }

    [Fact]
    public async Task EmptyQuery_ShouldDefaultToComplex()
    {
        // Arrange
        var query = "";
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.Equal(QueryType.Complex, plan.Type);
    }

    [Fact]
    public async Task VeryLongQuery_ShouldProcessSuccessfully()
    {
        // Arrange
        var query = string.Join(" ", Enumerable.Repeat("word", 200));
        var state = CreateConversationState();

        // Act
        var plan = await _controller.BuildQueryPlanAsync(query, state);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan.LayersToUse);
    }

    private static ConversationState CreateConversationState(int turnCount = 1)
    {
        return new ConversationState
        {
            UserId = "test_user",
            ConversationId = Guid.NewGuid().ToString(),
            MessageCount = turnCount,
            TurnCount = turnCount,
            ElapsedTime = TimeSpan.FromMinutes(5),
            QueryCount = turnCount,
            LastQueryTime = DateTime.UtcNow,
            AverageResponseTimeMs = 150,
            LastActivity = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Tests for smart heuristics-based importance scoring in AmygdalaImportanceEngine.
/// </summary>
public class AmygdalaImportanceEngineSmartHeuristicsTests
{
    private readonly AmygdalaImportanceEngine _engine;
    private readonly Mock<ILogger<AmygdalaImportanceEngine>> _mockLogger;

    public AmygdalaImportanceEngineSmartHeuristicsTests()
    {
        _mockLogger = new Mock<ILogger<AmygdalaImportanceEngine>>();
        _engine = new AmygdalaImportanceEngine(_mockLogger.Object);
    }

    [Theory]
    [InlineData("I decided we should use PostgreSQL for the database")]
    [InlineData("We committed to migrating to microservices")]
    [InlineData("I will implement the new caching strategy tomorrow")]
    public async Task DecisionLanguage_StrongDecisions_ShouldScoreHighImportance(string content)
    {
        // Arrange
        var message = CreateMessage(content);

        // Act
        var score = await _engine.CalculateImportanceAsync(message);

        // Assert
        Assert.True(score.FinalScore > 0.5, $"Decision messages should score > 0.5, got {score.FinalScore:F3}");
    }

    [Theory]
    [InlineData("CRITICAL: The database is down!")]
    [InlineData("This is essential for our security")]
    [InlineData("Remember to always validate user input")]
    public async Task ExplicitMarkers_CriticalKeywords_ShouldScoreVeryHighImportance(string content)
    {
        // Arrange
        var message = CreateMessage(content);

        // Act
        var score = await _engine.CalculateImportanceAsync(message);

        // Assert
        Assert.True(score.FinalScore > 0.6, $"Messages with critical markers should score > 0.6, got {score.FinalScore:F3}");
    }

    [Theory]
    [InlineData("```csharp\npublic void Method() {}\n```")]
    [InlineData("Here's the implementation: `var result = Calculate();`")]
    public async Task CodeBlocks_CodeContent_ShouldScoreHighImportance(string content)
    {
        // Arrange
        var message = CreateMessage(content);

        // Act
        var score = await _engine.CalculateImportanceAsync(message);

        // Assert
        Assert.True(score.FinalScore > 0.4, $"Code blocks should score > 0.4, got {score.FinalScore:F3}");
    }

    [Fact]
    public async Task Sentiment_ProblemReported_ShouldScoreHigherThanNeutral()
    {
        // Arrange
        var problemMessage = CreateMessage("There's a critical bug in production!");
        var neutralMessage = CreateMessage("The weather is nice today");

        // Act
        var problemScore = await _engine.CalculateImportanceAsync(problemMessage);
        var neutralScore = await _engine.CalculateImportanceAsync(neutralMessage);

        // Assert
        Assert.True(problemScore.FinalScore > neutralScore.FinalScore,
            $"Problem reports should score higher than neutral: {problemScore.FinalScore:F3} vs {neutralScore.FinalScore:F3}");
    }

    [Fact]
    public async Task TechnicalDepth_TechnicalVocabulary_ShouldBoostImportance()
    {
        // Arrange
        var technicalMessage = CreateMessage(
            "We need to optimize the algorithm for better performance. " +
            "The current architecture doesn't scale well with our API load.");
        var casualMessage = CreateMessage("Let's chat about the weather");

        // Act
        var technicalScore = await _engine.CalculateImportanceAsync(technicalMessage);
        var casualScore = await _engine.CalculateImportanceAsync(casualMessage);

        // Assert
        Assert.True(technicalScore.FinalScore > casualScore.FinalScore,
            $"Technical messages should score higher: {technicalScore.FinalScore:F3} vs {casualScore.FinalScore:F3}");
    }

    [Fact]
    public async Task Novelty_FirstMessage_ShouldBoostImportance()
    {
        // Arrange
        var firstMessage = CreateMessage("Starting a new project discussion", isFirst: true);
        var regularMessage = CreateMessage("Starting a new project discussion", isFirst: false);

        // Act
        var firstScore = await _engine.CalculateImportanceAsync(firstMessage);
        var regularScore = await _engine.CalculateImportanceAsync(regularMessage);

        // Assert
        Assert.True(firstScore.NoveltyBoost > regularScore.NoveltyBoost,
            "First messages should have higher novelty boost");
    }

    [Fact]
    public async Task Questions_DecisionQuestion_ShouldScoreHigherThanFactual()
    {
        // Arrange
        var decisionQuestion = CreateMessage("Should we migrate to the cloud?");
        var factualQuestion = CreateMessage("What time is it?");

        // Act
        var decisionScore = await _engine.CalculateImportanceAsync(decisionQuestion);
        var factualScore = await _engine.CalculateImportanceAsync(factualQuestion);

        // Assert
        Assert.True(decisionScore.FinalScore >= factualScore.FinalScore,
            $"Decision questions should score >= factual: {decisionScore.FinalScore:F3} vs {factualScore.FinalScore:F3}");
    }

    [Fact]
    public async Task Recency_RecentMessages_ShouldHaveHigherRecencyFactor()
    {
        // Arrange
        var recentMessage = CreateMessage("Recent message", timestamp: DateTime.UtcNow.AddMinutes(-5));
        var oldMessage = CreateMessage("Old message", timestamp: DateTime.UtcNow.AddDays(-30));

        // Act
        var recentScore = await _engine.CalculateImportanceAsync(recentMessage);
        var oldScore = await _engine.CalculateImportanceAsync(oldMessage);

        // Assert
        Assert.True(recentScore.RecencyFactor > oldScore.RecencyFactor,
            $"Recent messages should have higher recency: {recentScore.RecencyFactor:F3} vs {oldScore.RecencyFactor:F3}");
    }

    [Fact]
    public async Task EmptyContent_ShouldNotThrowException()
    {
        // Arrange
        var message = CreateMessage("");

        // Act & Assert
        var score = await _engine.CalculateImportanceAsync(message);
        Assert.NotNull(score);
        Assert.True(score.FinalScore >= 0 && score.FinalScore <= 1);
    }

    [Fact]
    public async Task VeryLongContent_ShouldProcessSuccessfully()
    {
        // Arrange
        var longContent = string.Join(" ", Enumerable.Repeat("word", 1000));
        var message = CreateMessage(longContent);

        // Act
        var score = await _engine.CalculateImportanceAsync(message);

        // Assert
        Assert.NotNull(score);
        Assert.True(score.FinalScore >= 0 && score.FinalScore <= 1);
    }

    [Theory]
    [InlineData("As we discussed earlier, this is important")]
    [InlineData("Previously, we decided to use Redis")]
    public async Task ConversationContext_ReferencesPrevious_ShouldBoostScore(string content)
    {
        // Arrange
        var message = CreateMessage(content);

        // Act
        var score = await _engine.CalculateImportanceAsync(message);

        // Assert
        Assert.True(score.FinalScore > 0.3, $"Context references should boost score, got {score.FinalScore:F3}");
    }

    private static Message CreateMessage(
        string content,
        bool isFirst = false,
        DateTime? timestamp = null)
    {
        var message = Message.Create(
            "test_user",
            Guid.NewGuid().ToString(),
            MessageRole.User,
            content);

        if (timestamp.HasValue)
        {
            // Use reflection to set timestamp for testing
            var timestampField = typeof(Message).GetField("<Timestamp>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timestampField?.SetValue(message, timestamp.Value);
        }

        if (isFirst)
        {
            // Mark as first message via metadata
            var metadataField = typeof(Message).GetField("<Metadata>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metadata = message.Metadata;
            var newMetadata = metadata with { Tags = new[] { "first_message" } };
            metadataField?.SetValue(message, newMetadata);
        }

        return message;
    }
}
