using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;
using MemoryKit.Infrastructure.Cognitive;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.Cognitive;

public class PrefrontalControllerServiceTests
{
    private readonly Mock<ISemanticKernelService> _llmMock;
    private readonly Mock<ILogger<PrefrontalController>> _baseLoggerMock;
    private readonly Mock<ILogger<PrefrontalControllerService>> _serviceLoggerMock;
    private readonly PrefrontalController _baseController;
    private readonly PrefrontalControllerService _service;

    public PrefrontalControllerServiceTests()
    {
        _llmMock = new Mock<ISemanticKernelService>();
        _baseLoggerMock = new Mock<ILogger<PrefrontalController>>();
        _serviceLoggerMock = new Mock<ILogger<PrefrontalControllerService>>();
        
        _baseController = new PrefrontalController(_baseLoggerMock.Object);
        _service = new PrefrontalControllerService(_baseController, _llmMock.Object, _serviceLoggerMock.Object);
    }

    [Fact]
    public async Task BuildQueryPlanAsync_DelegatesToBaseController()
    {
        // Arrange
        var query = "continue our conversation";
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

        // Act
        var plan = await _service.BuildQueryPlanAsync(query, state, CancellationToken.None);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(QueryType.Continuation, plan.Type);
        Assert.Contains(MemoryLayer.WorkingMemory, plan.LayersToUse);
        
        // Verify LLM was NOT called for simple continuation query
        _llmMock.Verify(x => x.ClassifyQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuildQueryPlanAsync_UsesLlmForAmbiguousQuery()
    {
        // Arrange
        var query = "something vague and unclear";
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

        // Mock LLM response for query classification
        _llmMock.Setup(x => x.ClassifyQueryAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("COMPLEX");

        // Act
        var plan = await _service.BuildQueryPlanAsync(query, state, CancellationToken.None);

        // Assert
        Assert.NotNull(plan);
        // Should have attempted LLM classification for ambiguous query
        _llmMock.Verify(x => x.ClassifyQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildQueryPlanAsync_FallsBackToBaseOnLlmFailure()
    {
        // Arrange
        var query = "some query";
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

        // Mock LLM to throw exception
        _llmMock.Setup(x => x.ClassifyQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        // Act
        var plan = await _service.BuildQueryPlanAsync(query, state, CancellationToken.None);

        // Assert - Should still get a valid plan from base controller (falls back to Complex)
        Assert.NotNull(plan);
        Assert.Equal(QueryType.Complex, plan.Type);
        Assert.NotNull(plan.LayersToUse);
        Assert.NotEmpty(plan.LayersToUse);
    }

    [Fact]
    public async Task BuildQueryPlanAsync_RecognizesFactRetrievalPattern()
    {
        // Arrange
        var query = "what is the user's email address?";
        var state = new ConversationState
        {
            UserId = "user123",
            ConversationId = "conv123",
            MessageCount = 10,
            LastActivity = DateTime.UtcNow
        };

        // Act
        var plan = await _service.BuildQueryPlanAsync(query, state, CancellationToken.None);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(QueryType.FactRetrieval, plan.Type);
        Assert.Contains(MemoryLayer.WorkingMemory, plan.LayersToUse);
        Assert.Contains(MemoryLayer.SemanticMemory, plan.LayersToUse);
    }

    [Fact]
    public async Task ClassifyQueryAsync_DelegatesToBaseController()
    {
        // Arrange
        var query = "continue the conversation";

        // Act
        var queryType = await _service.ClassifyQueryAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(QueryType.Continuation, queryType);
        
        // Verify LLM was NOT called for pattern-matched query
        _llmMock.Verify(x => x.ClassifyQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
