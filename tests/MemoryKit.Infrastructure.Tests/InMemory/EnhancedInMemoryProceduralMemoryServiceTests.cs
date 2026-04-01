using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.InMemory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.InMemory;

public class EnhancedInMemoryProceduralMemoryServiceTests
{
    private readonly Mock<ISemanticKernelService> _semanticKernelMock;
    private readonly Mock<ILogger<EnhancedInMemoryProceduralMemoryService>> _loggerMock;
    private readonly EnhancedInMemoryProceduralMemoryService _service;

    public EnhancedInMemoryProceduralMemoryServiceTests()
    {
        _loggerMock = new Mock<ILogger<EnhancedInMemoryProceduralMemoryService>>();
        _semanticKernelMock = new Mock<ISemanticKernelService>();
        _service = new EnhancedInMemoryProceduralMemoryService(
            _loggerMock.Object,
            _semanticKernelMock.Object);
    }

    [Fact]
    public async Task StorePatternAsync_CreateNewPattern_WhenNoSimilarExists()
    {
        // Arrange
        var userId = "user123";
        var pattern = ProceduralPattern.Create(
            userId,
            "CodeStyle",
            "User prefers Python with type hints",
            new[] { new PatternTrigger { Type = TriggerType.Regex, Pattern = "write.*code" } },
            "When writing code, use Python with type hints. Example: def func(x: int) -> str:");

        // Act
        await _service.StorePatternAsync(pattern);
        var userPatterns = await _service.GetUserPatternsAsync(userId);

        // Assert
        Assert.Single(userPatterns);
        Assert.Equal("CodeStyle", userPatterns[0].Name);
        Assert.Equal(1, userPatterns[0].UsageCount);
    }

    [Fact]
    public async Task StorePatternAsync_MergesWithExisting_WhenSimilarPatternExists()
    {
        // Arrange
        var userId = "user123";
        
        var existingPattern = ProceduralPattern.Create(
            userId,
            "CodeStyle",
            "User prefers Python",
            new[] { new PatternTrigger { Type = TriggerType.Regex, Pattern = "write.*code" } },
            "Use Python when writing code");

        var newPattern = ProceduralPattern.Create(
            userId,
            "codestyle",  // Same name, different casing
            "User prefers Python with type hints",
            new[] { new PatternTrigger { Type = TriggerType.Regex, Pattern = "code.*style" } },
            "When writing code, use Python with detailed type hints. Example: def func(x: int) -> str:");

        // Act
        await _service.StorePatternAsync(existingPattern);
        await _service.StorePatternAsync(newPattern);
        
        var userPatterns = await _service.GetUserPatternsAsync(userId);

        // Assert - Should have only 1 pattern (merged)
        Assert.Single(userPatterns);
        var mergedPattern = userPatterns[0];
        
        // Should increment usage count when merged
        Assert.Equal(2, mergedPattern.UsageCount);
        
        // Should have triggers from both patterns
        Assert.Equal(2, mergedPattern.Triggers.Length);
        
        // Should update to longer/better instruction template
        Assert.Contains("detailed type hints", mergedPattern.InstructionTemplate);
        Assert.True(mergedPattern.InstructionTemplate.Length > existingPattern.InstructionTemplate.Length);
    }

    [Fact]
    public async Task StorePatternAsync_DetectsDuplicates_BasedOnTriggerOverlap()
    {
        // Arrange
        var userId = "user123";
        
        var pattern1 = ProceduralPattern.Create(
            userId,
            "Pattern A",
            "First pattern",
            new[] 
            { 
                new PatternTrigger { Type = TriggerType.Keyword, Pattern = "python" },
                new PatternTrigger { Type = TriggerType.Keyword, Pattern = "type hints" }
            },
            "Use Python with type hints");

        var pattern2 = ProceduralPattern.Create(
            userId,
            "Pattern B",  // Different name
            "Second pattern",
            new[] 
            { 
                new PatternTrigger { Type = TriggerType.Keyword, Pattern = "python" },  // Same trigger
                new PatternTrigger { Type = TriggerType.Keyword, Pattern = "typing" }
            },
            "Use Python typing module for annotations");

        // Act
        await _service.StorePatternAsync(pattern1);
        await _service.StorePatternAsync(pattern2);
        
        var userPatterns = await _service.GetUserPatternsAsync(userId);

        // Assert - Should merge based on trigger overlap (>60%)
        Assert.Single(userPatterns);
        Assert.Equal(2, userPatterns[0].UsageCount);
    }

    [Fact]
    public async Task StorePatternAsync_UpdatesInstructionTemplate_WhenNewTemplateIsLonger()
    {
        // Arrange
        var userId = "user123";
        
        var shortPattern = ProceduralPattern.Create(
            userId,
            "CodeStyle",
            "Short description",
            new[] { new PatternTrigger { Type = TriggerType.Keyword, Pattern = "code" } },
            "Use Python");

        var detailedPattern = ProceduralPattern.Create(
            userId,
            "CodeStyle",
            "Detailed description",
            new[] { new PatternTrigger { Type = TriggerType.Keyword, Pattern = "code" } },
            "Use Python with type hints, docstrings, and follow PEP 8 style guidelines. Example: def process(data: List[int]) -> Dict[str, Any]:");

        // Act
        await _service.StorePatternAsync(shortPattern);
        await _service.StorePatternAsync(detailedPattern);
        
        var userPatterns = await _service.GetUserPatternsAsync(userId);

        // Assert - Should update to more detailed template
        Assert.Single(userPatterns);
        var pattern = userPatterns[0];
        Assert.Contains("PEP 8", pattern.InstructionTemplate);
        Assert.Contains("List[int]", pattern.InstructionTemplate);
        Assert.True(pattern.InstructionTemplate.Length > 100);
    }

    [Fact]
    public async Task StorePatternAsync_EnforcesMaxPatternsLimit()
    {
        // Arrange
        var userId = "user123";
        
        // Create 101 patterns (limit is 100)
        for (int i = 0; i < 101; i++)
        {
            var pattern = ProceduralPattern.Create(
                userId,
                $"Pattern{i}",
                $"Description {i}",
                new[] { new PatternTrigger { Type = TriggerType.Keyword, Pattern = $"trigger{i}" } },
                $"Instruction {i}");
                
            // Make first pattern have low usage
            if (i == 0)
            {
                // Don't record usage
            }
            else
            {
                pattern.RecordUsage();
                pattern.RecordUsage();
            }
            
            await _service.StorePatternAsync(pattern);
        }

        var userPatterns = await _service.GetUserPatternsAsync(userId);

        // Assert - Should have exactly 100 patterns (least used was removed)
        Assert.Equal(100, userPatterns.Length);
        
        // Pattern0 (least used) should be removed
        Assert.DoesNotContain(userPatterns, p => p.Name == "Pattern0");
    }

    [Fact]
    public async Task GetUserPatternsAsync_ReturnsEmptyArray_WhenUserHasNoPatterns()
    {
        // Arrange
        var userId = "nonexistent_user";

        // Act
        var patterns = await _service.GetUserPatternsAsync(userId);

        // Assert
        Assert.Empty(patterns);
    }

    [Fact]
    public async Task MatchPatternAsync_ReturnsNull_WhenNoPatternMatches()
    {
        // Arrange
        var userId = "user123";
        var message = Message.Create(userId, "conv123", MessageRole.User, "Hello world");

        // Act
        var match = await _service.MatchPatternAsync(userId, message.Content);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task DeleteUserDataAsync_RemovesAllPatternsForUser()
    {
        // Arrange
        var userId = "user123";
        var pattern = ProceduralPattern.Create(
            userId,
            "TestPattern",
            "Test description",
            new[] { new PatternTrigger { Type = TriggerType.Keyword, Pattern = "test" } },
            "Test instruction");

        await _service.StorePatternAsync(pattern);
        var beforeDelete = await _service.GetUserPatternsAsync(userId);
        Assert.Single(beforeDelete);

        // Act
        await _service.DeleteUserDataAsync(userId);
        var afterDelete = await _service.GetUserPatternsAsync(userId);

        // Assert
        Assert.Empty(afterDelete);
    }
}
