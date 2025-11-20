using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using Xunit;

namespace MemoryKit.Domain.Tests.Entities;

public class ExtractedFactTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsExtractedFact()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var key = "user_name";
        var value = "John Doe";
        var type = EntityType.Person;
        var importance = 0.8;

        // Act
        var fact = ExtractedFact.Create(userId, conversationId, key, value, type, importance);

        // Assert
        Assert.NotNull(fact);
        Assert.Equal(userId, fact.UserId);
        Assert.Equal(conversationId, fact.ConversationId);
        Assert.Equal(key, fact.Key);
        Assert.Equal(value, fact.Value);
        Assert.Equal(type, fact.Type);
        Assert.Equal(importance, fact.Importance);
        Assert.Equal(1, fact.AccessCount);
        Assert.NotEmpty(fact.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ExtractedFact.Create(userId!, "conv123", "key", "value", EntityType.Other));
        Assert.Contains("User ID", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidKey_ThrowsArgumentException(string? key)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ExtractedFact.Create("user123", "conv123", key!, "value", EntityType.Other));
        Assert.Contains("Key", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidValue_ThrowsArgumentException(string? value)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ExtractedFact.Create("user123", "conv123", "key", value!, EntityType.Other));
        Assert.Contains("Value", exception.Message);
    }

    [Theory]
    [InlineData(-0.1, 0.0)]
    [InlineData(1.1, 1.0)]
    [InlineData(0.5, 0.5)]
    public void Create_ClampsImportanceScore(double input, double expected)
    {
        // Act
        var fact = ExtractedFact.Create("user123", "conv123", "key", "value", EntityType.Other, input);

        // Assert
        Assert.Equal(expected, fact.Importance);
    }

    [Fact]
    public void RecordAccess_IncrementsAccessCount()
    {
        // Arrange
        var fact = ExtractedFact.Create("user123", "conv123", "key", "value", EntityType.Other);
        var initialAccessTime = fact.LastAccessed;
        var initialAccessCount = fact.AccessCount;

        // Wait a tiny bit to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        fact.RecordAccess();

        // Assert
        Assert.Equal(initialAccessCount + 1, fact.AccessCount);
        Assert.True(fact.LastAccessed > initialAccessTime);
    }

    [Fact]
    public void ShouldEvict_WithOldUnusedFact_ReturnsTrue()
    {
        // Arrange
        var fact = ExtractedFact.Create("user123", "conv123", "key", "value", EntityType.Other);
        var ttl = TimeSpan.FromSeconds(1);
        var minAccessCount = 5;

        // Wait for TTL to expire
        Thread.Sleep(1100);

        // Act
        var shouldEvict = fact.ShouldEvict(ttl, minAccessCount);

        // Assert
        Assert.True(shouldEvict);
    }

    [Fact]
    public void ShouldEvict_WithFrequentlyAccessedFact_ReturnsFalse()
    {
        // Arrange
        var fact = ExtractedFact.Create("user123", "conv123", "key", "value", EntityType.Other);

        // Access multiple times
        for (int i = 0; i < 10; i++)
        {
            fact.RecordAccess();
        }

        var ttl = TimeSpan.FromSeconds(1);
        var minAccessCount = 5;

        // Wait for TTL to expire
        Thread.Sleep(1100);

        // Act
        var shouldEvict = fact.ShouldEvict(ttl, minAccessCount);

        // Assert
        Assert.False(shouldEvict); // Frequently accessed, don't evict despite age
    }

    [Fact]
    public void UpdateImportance_UpdatesScoreAndTimestamp()
    {
        // Arrange
        var fact = ExtractedFact.Create("user123", "conv123", "key", "value", EntityType.Other, 0.5);
        var initialUpdatedAt = fact.UpdatedAt;

        Thread.Sleep(100); // Increased to ensure timestamp difference

        // Act
        fact.UpdateImportance(0.9);

        // Assert
        Assert.Equal(0.9, fact.Importance);
        Assert.NotNull(fact.UpdatedAt);
        Assert.True(fact.UpdatedAt.Value > (initialUpdatedAt ?? DateTime.MinValue), 
            $"Expected UpdatedAt ({fact.UpdatedAt}) to be greater than initial ({initialUpdatedAt})");
    }

    [Fact]
    public void SetEmbedding_StoresEmbeddingVector()
    {
        // Arrange
        var fact = ExtractedFact.Create("user123", "conv123", "key", "value", EntityType.Other);
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        fact.SetEmbedding(embedding);

        // Assert
        Assert.NotNull(fact.Embedding);
        Assert.Equal(embedding.Length, fact.Embedding.Length);
        Assert.Equal(embedding, fact.Embedding);
    }
}
