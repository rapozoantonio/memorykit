using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemoryKit.Application.Tests.Services;

public class MemoryOrchestratorTests
{
    private readonly Mock<IWorkingMemoryService> _workingMemoryMock;
    private readonly Mock<IScratchpadService> _scratchpadMock;
    private readonly Mock<IEpisodicMemoryService> _episodicMemoryMock;
    private readonly Mock<IProceduralMemoryService> _proceduralMemoryMock;
    private readonly Mock<IPrefrontalController> _prefrontalMock;
    private readonly Mock<IAmygdalaImportanceEngine> _amygdalaMock;
    private readonly Mock<ISemanticKernelService> _semanticKernelMock;
    private readonly Mock<ILogger<MemoryOrchestrator>> _loggerMock;
    private readonly MemoryOrchestrator _orchestrator;

    public MemoryOrchestratorTests()
    {
        _workingMemoryMock = new Mock<IWorkingMemoryService>();
        _scratchpadMock = new Mock<IScratchpadService>();
        _episodicMemoryMock = new Mock<IEpisodicMemoryService>();
        _proceduralMemoryMock = new Mock<IProceduralMemoryService>();
        _prefrontalMock = new Mock<IPrefrontalController>();
        _amygdalaMock = new Mock<IAmygdalaImportanceEngine>();
        _semanticKernelMock = new Mock<ISemanticKernelService>();
        _loggerMock = new Mock<ILogger<MemoryOrchestrator>>();

        _orchestrator = new MemoryOrchestrator(
            _workingMemoryMock.Object,
            _scratchpadMock.Object,
            _episodicMemoryMock.Object,
            _proceduralMemoryMock.Object,
            _prefrontalMock.Object,
            _amygdalaMock.Object,
            _semanticKernelMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task StoreAsync_CalculatesImportanceAndStoresInAllLayers()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Important message");

        var importanceScore = new ImportanceScore
        {
            BaseScore = 0.8,
            EmotionalWeight = 0.3,
            NoveltyBoost = 0.2,
            RecencyFactor = 1.0
        };

        _amygdalaMock
            .Setup(x => x.CalculateImportanceAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importanceScore);

        // Act
        await _orchestrator.StoreAsync(userId, conversationId, message, CancellationToken.None);

        // Assert
        _amygdalaMock.Verify(x => x.CalculateImportanceAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        _episodicMemoryMock.Verify(x => x.ArchiveAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        _workingMemoryMock.Verify(x => x.AddAsync(userId, conversationId, message, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(message.Metadata.ImportanceScore > 0);
    }

    [Fact]
    public async Task RetrieveContextAsync_QueriesMultipleLayersInParallel()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var query = "What did we discuss?";

        var workingMessages = new[] { Message.Create(userId, conversationId, MessageRole.User, "Recent message") };
        var facts = new[] { ExtractedFact.Create(userId, conversationId, "key", "value", EntityType.Other) };
        var archivedMessages = new[] { Message.Create(userId, conversationId, MessageRole.User, "Old message") };

        var queryPlan = new QueryPlan
        {
            Type = QueryType.FactRetrieval,
            LayersToUse = new List<MemoryLayer> { MemoryLayer.WorkingMemory, MemoryLayer.SemanticMemory },
            EstimatedTokens = 500,
            SuggestedProcedureId = null
        };

        _prefrontalMock
            .Setup(x => x.BuildQueryPlanAsync(query, It.IsAny<ConversationState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryPlan);

        _workingMemoryMock
            .Setup(x => x.GetRecentAsync(userId, conversationId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workingMessages);

        _scratchpadMock
            .Setup(x => x.SearchFactsAsync(userId, query, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(facts);

        _episodicMemoryMock
            .Setup(x => x.SearchAsync(userId, query, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(archivedMessages);

        _proceduralMemoryMock
            .Setup(x => x.MatchPatternAsync(userId, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProceduralPattern?)null);

        // Act
        var context = await _orchestrator.RetrieveContextAsync(userId, conversationId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(queryPlan, context.QueryPlan);
        Assert.Single(context.WorkingMemory);
        Assert.Single(context.Facts);
        Assert.True(context.RetrievalLatencyMs >= 0);
    }

    [Fact]
    public async Task DeleteUserDataAsync_DeletesFromAllLayers()
    {
        // Arrange
        var userId = "user123";

        _workingMemoryMock
            .Setup(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _scratchpadMock
            .Setup(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _episodicMemoryMock
            .Setup(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _proceduralMemoryMock
            .Setup(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orchestrator.DeleteUserDataAsync(userId, CancellationToken.None);

        // Assert
        _workingMemoryMock.Verify(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _scratchpadMock.Verify(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _episodicMemoryMock.Verify(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _proceduralMemoryMock.Verify(x => x.DeleteUserDataAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildQueryPlanAsync_DelegatesToPrefrontalController()
    {
        // Arrange
        var query = "Test query";
        var state = new ConversationState
        {
            UserId = "user123",
            ConversationId = "conv123",
            MessageCount = 5,
            TurnCount = 2,
            ElapsedTime = TimeSpan.FromMinutes(5),
            QueryCount = 3,
            LastQueryTime = DateTime.UtcNow.AddMinutes(-1),
            AverageResponseTimeMs = 150,
            LastActivity = DateTime.UtcNow
        };

        var expectedPlan = new QueryPlan
        {
            Type = QueryType.Complex,
            LayersToUse = new List<MemoryLayer> { MemoryLayer.WorkingMemory, MemoryLayer.SemanticMemory, MemoryLayer.EpisodicMemory },
            EstimatedTokens = 2000,
            SuggestedProcedureId = null
        };

        _prefrontalMock
            .Setup(x => x.BuildQueryPlanAsync(query, state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlan);

        // Act
        var plan = await _orchestrator.BuildQueryPlanAsync(query, state, CancellationToken.None);

        // Assert
        Assert.Equal(expectedPlan, plan);
        _prefrontalMock.Verify(x => x.BuildQueryPlanAsync(query, state, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StoreAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Mock amygdala to throw OperationCanceledException
        _amygdalaMock
            .Setup(x => x.CalculateImportanceAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _orchestrator.StoreAsync(userId, conversationId, message, cts.Token));
    }

    [Fact]
    public async Task StoreSemanticFactsAsync_StoresFactsInScratchpad()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var facts = new[]
        {
            ExtractedFact.Create(userId, conversationId, "name", "John", EntityType.Person, 0.8),
            ExtractedFact.Create(userId, conversationId, "language", "Python", EntityType.Other, 0.7)
        };

        _scratchpadMock
            .Setup(x => x.StoreFactsAsync(userId, conversationId, facts, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orchestrator.StoreSemanticFactsAsync(userId, conversationId, facts, CancellationToken.None);

        // Assert
        _scratchpadMock.Verify(
            x => x.StoreFactsAsync(userId, conversationId, facts, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreSemanticFactsAsync_DoesNothing_WhenFactsArrayIsEmpty()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var facts = Array.Empty<ExtractedFact>();

        // Act
        await _orchestrator.StoreSemanticFactsAsync(userId, conversationId, facts, CancellationToken.None);

        // Assert - Should not call StoreFactsAsync when array is empty
        _scratchpadMock.Verify(
            x => x.StoreFactsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ExtractedFact[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StoreAsync_ExtractsSemanticFactsSuccessfully()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "I love Python programming");

        var importanceScore = new ImportanceScore
        {
            BaseScore = 0.7,
            EmotionalWeight = 0.2,
            NoveltyBoost = 0.1,
            RecencyFactor = 1.0
        };

        var extractedEntities = new[]
        {
            new ExtractedEntity
            {
                Key = "preference",
                Value = "Python programming",
                Type = EntityType.Preference,
                Importance = 0.8
            }
        };

        _amygdalaMock
            .Setup(x => x.CalculateImportanceAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importanceScore);

        _semanticKernelMock
            .Setup(x => x.ExtractEntitiesAsync(message.Content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedEntities);

        // Act
        await _orchestrator.StoreAsync(userId, conversationId, message, CancellationToken.None);

        // Assert
        _semanticKernelMock.Verify(
            x => x.ExtractEntitiesAsync(message.Content, It.IsAny<CancellationToken>()),
            Times.Once);

        _scratchpadMock.Verify(
            x => x.StoreFactsAsync(
                userId,
                conversationId,
                It.Is<ExtractedFact[]>(facts =>
                    facts.Length == 1 &&
                    facts[0].Key == "preference" &&
                    facts[0].Value == "Python programming"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreAsync_UsesFallbackFact_WhenExtractionTimesOut()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Test message content");

        var importanceScore = new ImportanceScore
        {
            BaseScore = 0.5,
            EmotionalWeight = 0.1,
            NoveltyBoost = 0.0,
            RecencyFactor = 1.0
        };

        _amygdalaMock
            .Setup(x => x.CalculateImportanceAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importanceScore);

        _semanticKernelMock
            .Setup(x => x.ExtractEntitiesAsync(message.Content, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Timeout"));

        // Act
        await _orchestrator.StoreAsync(userId, conversationId, message, CancellationToken.None);

        // Assert - Should create fallback fact
        _scratchpadMock.Verify(
            x => x.StoreFactsAsync(
                userId,
                conversationId,
                It.Is<ExtractedFact[]>(facts =>
                    facts.Length == 1 &&
                    facts[0].Key == "statement" &&
                    facts[0].Value == message.Content.Trim() &&
                    facts[0].Type == EntityType.Other &&
                    facts[0].Importance == 0.5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreAsync_UsesFallbackFact_WhenExtractionReturnsEmpty()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Simple message");

        var importanceScore = new ImportanceScore
        {
            BaseScore = 0.5,
            EmotionalWeight = 0.0,
            NoveltyBoost = 0.0,
            RecencyFactor = 1.0
        };

        _amygdalaMock
            .Setup(x => x.CalculateImportanceAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importanceScore);

        _semanticKernelMock
            .Setup(x => x.ExtractEntitiesAsync(message.Content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExtractedEntity>());

        // Act
        await _orchestrator.StoreAsync(userId, conversationId, message, CancellationToken.None);

        // Assert - Should create fallback fact when extraction returns empty
        _scratchpadMock.Verify(
            x => x.StoreFactsAsync(
                userId,
                conversationId,
                It.Is<ExtractedFact[]>(facts =>
                    facts.Length == 1 &&
                    facts[0].Key == "statement" &&
                    facts[0].Value == message.Content.Trim()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreAsync_UsesFallbackFact_WhenExtractionThrowsException()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Test content");

        var importanceScore = new ImportanceScore
        {
            BaseScore = 0.5,
            EmotionalWeight = 0.0,
            NoveltyBoost = 0.0,
            RecencyFactor = 1.0
        };

        _amygdalaMock
            .Setup(x => x.CalculateImportanceAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importanceScore);

        _semanticKernelMock
            .Setup(x => x.ExtractEntitiesAsync(message.Content, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        // Act
        await _orchestrator.StoreAsync(userId, conversationId, message, CancellationToken.None);

        // Assert - Should create fallback fact on any exception
        _scratchpadMock.Verify(
            x => x.StoreFactsAsync(
                userId,
                conversationId,
                It.Is<ExtractedFact[]>(facts =>
                    facts.Length == 1 &&
                    facts[0].Key == "statement"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreAsync_StoresInAllThreeLayers()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Multi-layer test");

        var importanceScore = new ImportanceScore
        {
            BaseScore = 0.6,
            EmotionalWeight = 0.1,
            NoveltyBoost = 0.0,
            RecencyFactor = 1.0
        };

        _amygdalaMock
            .Setup(x => x.CalculateImportanceAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importanceScore);

        _semanticKernelMock
            .Setup(x => x.ExtractEntitiesAsync(message.Content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExtractedEntity>());

        // Act
        await _orchestrator.StoreAsync(userId, conversationId, message, CancellationToken.None);

        // Assert - Verify all three layers are written to
        _episodicMemoryMock.Verify(
            x => x.ArchiveAsync(message, It.IsAny<CancellationToken>()),
            Times.Once,
            "L1 Episodic should be written to");

        _scratchpadMock.Verify(
            x => x.StoreFactsAsync(userId, conversationId, It.IsAny<ExtractedFact[]>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "L2 Semantic should be written to");

        _workingMemoryMock.Verify(
            x => x.AddAsync(userId, conversationId, message, It.IsAny<CancellationToken>()),
            Times.Once,
            "L3 Working Memory should be written to");
    }
}
