using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MemoryKit.Application.Configuration;
using MemoryKit.Application.UseCases.AddMessage;
using MemoryKit.Application.DTOs;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Domain.ValueObjects;

namespace MemoryKit.Application.Tests.Integration;

public class HeuristicExtractionIntegrationTests
{
    [Fact]
    public async Task AddMessage_HeuristicOnly_ShouldNotCallLLM()
    {
        // Arrange: Configure HeuristicOnly=true
        var configData = new Dictionary<string, string?>
        {
            ["MemoryKit:HeuristicExtraction:HeuristicOnly"] = "true",
            ["MemoryKit:HeuristicExtraction:UseHeuristicFirst"] = "true",
            ["MemoryKit:HeuristicExtraction:LogExtractionMethod"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var llmMock = new Mock<ISemanticKernelService>();
        var orchestratorMock = new Mock<IMemoryOrchestrator>();
        var loggerMock = new Mock<ILogger<AddMessageHandler>>();

        orchestratorMock
            .Setup(x => x.StoreAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        orchestratorMock
            .Setup(x => x.StoreSemanticFactsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ExtractedFact[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AddMessageHandler(
            orchestratorMock.Object,
            llmMock.Object,
            loggerMock.Object,
            configuration
        );

        var request = new AddMessageCommand(
            "user1",
            "conv1",
            new CreateMessageRequest
            {
                Role = MessageRole.User,
                Content = "My name is Alice. I prefer PostgreSQL over MySQL."
            }
        );

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        // Wait for background task to complete
        await Task.Delay(200);

        // Assert: LLM should NEVER be called
        llmMock.Verify(
            x => x.ExtractEntitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // Facts should still be stored (2 facts: Alice + PostgreSQL)
        orchestratorMock.Verify(
            x => x.StoreSemanticFactsAsync(
                "user1",
                "conv1",
                It.Is<ExtractedFact[]>(facts => facts.Length == 2),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AddMessage_HeuristicInsufficientFacts_ShouldFallbackToLLM()
    {
        // Arrange: MinHeuristicFactsForAI=3, but message only has 1 fact
        var configData = new Dictionary<string, string?>
        {
            ["MemoryKit:HeuristicExtraction:UseHeuristicFirst"] = "true",
            ["MemoryKit:HeuristicExtraction:HeuristicOnly"] = "false",
            ["MemoryKit:HeuristicExtraction:MinHeuristicFactsForAI"] = "3",
            ["MemoryKit:HeuristicExtraction:LogExtractionMethod"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var llmMock = new Mock<ISemanticKernelService>();
        llmMock.Setup(x => x.ExtractEntitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ExtractedEntity
                {
                    Key = "Technology",
                    Value = "Redis",
                    Type = EntityType.Technology,
                    Importance = 0.7,
                    IsNovel = true,
                    Embedding = new float[384]
                }
            });

        var orchestratorMock = new Mock<IMemoryOrchestrator>();
        var loggerMock = new Mock<ILogger<AddMessageHandler>>();

        orchestratorMock
            .Setup(x => x.StoreAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        orchestratorMock
            .Setup(x => x.StoreSemanticFactsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ExtractedFact[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AddMessageHandler(
            orchestratorMock.Object,
            llmMock.Object,
            loggerMock.Object,
            configuration
        );

        var request = new AddMessageCommand(
            "user1",
            "conv1",
            new CreateMessageRequest
            {
                Role = MessageRole.User,
                Content = "I prefer clean code." // Only 1 heuristic fact
            }
        );

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        // Wait for background task
        await Task.Delay(200);

        // Assert: LLM SHOULD be called because heuristics only found 1 fact (< 3 threshold)
        llmMock.Verify(
            x => x.ExtractEntitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Merged facts should be stored (heuristic + LLM)
        orchestratorMock.Verify(
            x => x.StoreSemanticFactsAsync(
                "user1",
                "conv1",
                It.Is<ExtractedFact[]>(facts => facts.Length >= 1),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AddMessage_HeuristicSufficientFacts_ShouldSkipLLM()
    {
        // Arrange: MinHeuristicFactsForAI=2, message has 3 facts
        var configData = new Dictionary<string, string?>
        {
            ["MemoryKit:HeuristicExtraction:UseHeuristicFirst"] = "true",
            ["MemoryKit:HeuristicExtraction:HeuristicOnly"] = "false",
            ["MemoryKit:HeuristicExtraction:MinHeuristicFactsForAI"] = "2",
            ["MemoryKit:HeuristicExtraction:LogExtractionMethod"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var llmMock = new Mock<ISemanticKernelService>();
        var orchestratorMock = new Mock<IMemoryOrchestrator>();
        var loggerMock = new Mock<ILogger<AddMessageHandler>>();

        orchestratorMock
            .Setup(x => x.StoreAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        orchestratorMock
            .Setup(x => x.StoreSemanticFactsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ExtractedFact[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AddMessageHandler(
            orchestratorMock.Object,
            llmMock.Object,
            loggerMock.Object,
            configuration
        );

        var request = new AddMessageCommand(
            "user1",
            "conv1",
            new CreateMessageRequest
            {
                Role = MessageRole.User,
                Content = "My name is Bob. I prefer Docker. We decided to use Redis."
            }
        );

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        // Wait for background task
        await Task.Delay(200);

        // Assert: LLM should NOT be called (3 facts >= 2 threshold)
        llmMock.Verify(
            x => x.ExtractEntitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // 3 heuristic facts should be stored
        orchestratorMock.Verify(
            x => x.StoreSemanticFactsAsync(
                "user1",
                "conv1",
                It.Is<ExtractedFact[]>(facts => facts.Length == 3),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AddMessage_LLMOnlyMode_ShouldOnlyCallLLM()
    {
        // Arrange: UseHeuristicFirst=false (traditional behavior)
        var configData = new Dictionary<string, string?>
        {
            ["MemoryKit:HeuristicExtraction:UseHeuristicFirst"] = "false",
            ["MemoryKit:HeuristicExtraction:HeuristicOnly"] = "false",
            ["MemoryKit:HeuristicExtraction:LogExtractionMethod"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var llmMock = new Mock<ISemanticKernelService>();
        llmMock.Setup(x => x.ExtractEntitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ExtractedEntity
                {
                    Key = "Person",
                    Value = "Charlie",
                    Type = EntityType.Person,
                    Importance = 0.8,
                    IsNovel = true,
                    Embedding = new float[384]
                }
            });

        var orchestratorMock = new Mock<IMemoryOrchestrator>();
        var loggerMock = new Mock<ILogger<AddMessageHandler>>();

        orchestratorMock
            .Setup(x => x.StoreAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        orchestratorMock
            .Setup(x => x.StoreSemanticFactsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ExtractedFact[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AddMessageHandler(
            orchestratorMock.Object,
            llmMock.Object,
            loggerMock.Object,
            configuration
        );

        var request = new AddMessageCommand(
            "user1",
            "conv1",
            new CreateMessageRequest
            {
                Role = MessageRole.User,
                Content = "My name is Charlie. I prefer Python."
            }
        );

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        // Wait for background task
        await Task.Delay(200);

        // Assert: LLM SHOULD be called (original behavior)
        llmMock.Verify(
            x => x.ExtractEntitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // LLM facts should be stored
        orchestratorMock.Verify(
            x => x.StoreSemanticFactsAsync(
                "user1",
                "conv1",
                It.Is<ExtractedFact[]>(facts => facts.Length >= 1),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HeuristicOnly_BiographicalNarrative_ExtractsNarrativeFacts()
    {
        // Arrange: Configure heuristic-only mode with narrative fallback
        var configData = new Dictionary<string, string?>
        {
            ["MemoryKit:HeuristicExtraction:HeuristicOnly"] = "true",
            ["MemoryKit:HeuristicExtraction:UseNarrativeFallback"] = "true",
            ["MemoryKit:HeuristicExtraction:LogExtractionMethod"] = "true",
            ["MemoryKit:HeuristicExtraction:MaxNarrativeFragmentsPerMessage"] = "3",
            ["MemoryKit:HeuristicExtraction:NarrativeImportanceScore"] = "0.50"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var orchestratorMock = new Mock<IMemoryOrchestrator>();
        var llmMock = new Mock<ISemanticKernelService>();
        var loggerMock = new Mock<ILogger<AddMessageHandler>>();

        orchestratorMock
            .Setup(x => x.StoreAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Message>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        orchestratorMock
            .Setup(x => x.StoreSemanticFactsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ExtractedFact[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AddMessageHandler(
            orchestratorMock.Object,
            llmMock.Object,
            loggerMock.Object,
            configuration
        );

        var request = new AddMessageCommand(
            "user1",
            "conv1",
            new CreateMessageRequest
            {
                Role = MessageRole.User,
                Content = "My first time riding a bicycle I was 7 years old. It was in the playground of the first apartment I lived in, in Niterói."
            }
        );

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        // Wait for background task to complete
        await Task.Delay(200);

        // Assert: LLM should NEVER be called (heuristic-only mode)
        llmMock.Verify(
            x => x.ExtractEntitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // Narrative facts should be stored
        // Expected: 1 Memory entity + 1 Age entity + 1 Location (Niterói) = ~3 entities
        orchestratorMock.Verify(
            x => x.StoreSemanticFactsAsync(
                "user1",
                "conv1",
                It.Is<ExtractedFact[]>(facts =>
                    facts.Length >= 2 &&
                    facts.Any(f => f.Key == "Memory" && f.Type == EntityType.Other) &&
                    facts.Any(f => f.Key == "Age") &&
                    facts.Any(f => f.Key == "Location" && f.Type == EntityType.Place)),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
