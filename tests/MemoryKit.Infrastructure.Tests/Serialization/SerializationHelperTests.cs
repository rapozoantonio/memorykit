using System.Text.Json;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.Serialization;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.Serialization;

/// <summary>
/// Tests for SerializationHelper to verify optimized JSON serialization
/// reduces storage size while maintaining data integrity.
/// </summary>
public class SerializationHelperTests
{
    [Fact]
    public void Serialize_Message_ProducesCompactJson()
    {
        // Arrange
        var message = Message.Create(
            userId: "user-123",
            conversationId: "conv-456",
            role: MessageRole.User,
            content: "What is the weather today?");

        // Act
        var optimizedJson = SerializationHelper.Serialize(message);
        var defaultJson = JsonSerializer.Serialize(message);

        // Assert
        Assert.NotEmpty(optimizedJson);
        Assert.True(optimizedJson.Length < defaultJson.Length,
            $"Optimized JSON ({optimizedJson.Length} bytes) should be smaller than default JSON ({defaultJson.Length} bytes)");
        
        // Verify compact format (no whitespace)
        Assert.DoesNotContain("\n", optimizedJson);
        Assert.DoesNotContain("  ", optimizedJson);
    }

    [Fact]
    public void Deserialize_Message_RoundTripsSuccessfully()
    {
        // Arrange
        var original = Message.Create(
            userId: "user-123",
            conversationId: "conv-456",
            role: MessageRole.User,
            content: "Test message with special characters: 测试 émojis 🚀");

        // Act
        var json = SerializationHelper.Serialize(original);
        var deserialized = SerializationHelper.Deserialize<Message>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized!.Id);
        Assert.Equal(original.UserId, deserialized.UserId);
        Assert.Equal(original.ConversationId, deserialized.ConversationId);
        Assert.Equal(original.Role, deserialized.Role);
        Assert.Equal(original.Content, deserialized.Content);
        Assert.Equal(original.Timestamp.ToString("s"), deserialized.Timestamp.ToString("s")); // Compare to second precision
    }

    [Fact]
    public void DateTimeConverter_SerializesAsUnixTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2025, 12, 9, 10, 30, 45, DateTimeKind.Utc);
        var expected = new DateTimeOffset(dateTime).ToUnixTimeSeconds();

        // Act
        var json = SerializationHelper.Serialize(new { timestamp = dateTime });

        // Assert - Should contain Unix timestamp (number), not ISO string
        Assert.Contains(expected.ToString(), json);
        Assert.DoesNotContain("2025-12-09", json); // ISO format should not be present
    }

    [Fact]
    public void DateTimeConverter_DeserializesFromUnixTimestamp()
    {
        // Arrange
        var expectedDateTime = new DateTime(2025, 12, 9, 10, 30, 0, DateTimeKind.Utc);
        var unixTimestamp = new DateTimeOffset(expectedDateTime).ToUnixTimeSeconds();
        var json = $"{{\"timestamp\":{unixTimestamp}}}";

        // Act
        var result = SerializationHelper.Deserialize<TestClass>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDateTime, result!.Timestamp);
    }

    [Fact]
    public void DateTimeConverter_BackwardCompatibleWithIsoStrings()
    {
        // Arrange - Old format with ISO 8601 strings
        var json = "{\"timestamp\":\"2025-12-09T10:30:45Z\"}";

        // Act
        var result = SerializationHelper.Deserialize<TestClass>(json);

        // Assert - Should still deserialize correctly
        Assert.NotNull(result);
        Assert.Equal(2025, result!.Timestamp.Year);
        Assert.Equal(12, result.Timestamp.Month);
        Assert.Equal(9, result.Timestamp.Day);
    }

    [Fact]
    public void Serialize_NullValues_AreOmitted()
    {
        // Arrange
        var obj = new { name = "test", nullValue = (string?)null, validValue = "present" };

        // Act
        var json = SerializationHelper.Serialize(obj);

        // Assert
        Assert.DoesNotContain("nullValue", json); // Null properties should be omitted
        Assert.Contains("name", json);
        Assert.Contains("validValue", json);
    }

    [Fact]
    public void Serialize_Enum_AsString()
    {
        // Arrange
        var message = Message.Create(
            userId: "user-123",
            conversationId: "conv-456",
            role: MessageRole.Assistant,
            content: "Test");

        // Act
        var json = SerializationHelper.Serialize(message);

        // Assert - Enum should be serialized as camelCase string for readability
        Assert.Contains("\"assistant\"", json.ToLowerInvariant());
    }

    [Fact]
    public void SerializeToUtf8Bytes_ProducesSameResultAsString()
    {
        // Arrange
        var message = Message.Create(
            userId: "user-123",
            conversationId: "conv-456",
            role: MessageRole.User,
            content: "Test message");

        // Act
        var jsonString = SerializationHelper.Serialize(message);
        var jsonBytes = SerializationHelper.SerializeToUtf8Bytes(message);
        var bytesAsString = System.Text.Encoding.UTF8.GetString(jsonBytes);

        // Assert
        Assert.Equal(jsonString, bytesAsString);
    }

    [Fact]
    public void Deserialize_FromUtf8Bytes_WorksCorrectly()
    {
        // Arrange
        var original = Message.Create(
            userId: "user-123",
            conversationId: "conv-456",
            role: MessageRole.User,
            content: "Test message");

        var bytes = SerializationHelper.SerializeToUtf8Bytes(original);

        // Act
        var deserialized = SerializationHelper.Deserialize<Message>(bytes);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized!.Id);
        Assert.Equal(original.Content, deserialized.Content);
    }

    [Fact]
    public void Serialize_ExtractedFact_PreservesAllProperties()
    {
        // Arrange
        var fact = ExtractedFact.Create(
            userId: "user-123",
            conversationId: "conv-456",
            key: "user_preference",
            value: "Prefers PostgreSQL for databases",
            type: EntityType.Preference,
            importance: 0.85);

        // Act
        var json = SerializationHelper.Serialize(fact);
        var deserialized = SerializationHelper.Deserialize<ExtractedFact>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(fact.Key, deserialized!.Key);
        Assert.Equal(fact.Value, deserialized.Value);
        Assert.Equal(fact.Type, deserialized.Type);
        Assert.Equal(fact.Importance, deserialized.Importance);
    }

    [Fact]
    public void Serialize_FloatArray_IsCompact()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

        // Act
        var optimizedJson = SerializationHelper.Serialize(embedding);
        var defaultJson = JsonSerializer.Serialize(embedding);

        // Assert
        Assert.True(optimizedJson.Length <= defaultJson.Length);
        
        // Verify all values are preserved
        var deserialized = SerializationHelper.Deserialize<float[]>(optimizedJson);
        Assert.NotNull(deserialized);
        Assert.Equal(embedding.Length, deserialized!.Length);
        Assert.Equal(embedding, deserialized);
    }

    [Fact]
    public void Serialize_LargeMessage_ShowsSignificantSizeReduction()
    {
        // Arrange - Simulate a large message with code
        var largeContent = @"
Here's a comprehensive implementation:

```csharp
public class DatabaseService
{
    private readonly IConfiguration _config;
    
    public DatabaseService(IConfiguration config)
    {
        _config = config;
    }
    
    public async Task<User> GetUserAsync(string userId)
    {
        // Implementation here
        return new User();
    }
}
```

This demonstrates the pattern you requested.";

        var message = Message.Create(
            userId: "user-123",
            conversationId: "conv-456",
            role: MessageRole.Assistant,
            content: largeContent);

        // Act
        var optimizedJson = SerializationHelper.Serialize(message);
        var defaultJson = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });

        // Assert
        var reductionPercentage = (1 - (double)optimizedJson.Length / defaultJson.Length) * 100;
        
        Assert.True(reductionPercentage > 20, 
            $"Expected >20% size reduction, got {reductionPercentage:F1}%. " +
            $"Optimized: {optimizedJson.Length} bytes, Default: {defaultJson.Length} bytes");
    }

    [Fact]
    public void Serialize_ProceduralPattern_MaintainsComplexStructure()
    {
        // Arrange
        var pattern = ProceduralPattern.Create(
            userId: "user-123",
            name: "database_selection",
            description: "Pattern for selecting databases",
            triggers: new[]
            {
                new PatternTrigger
                {
                    Type = TriggerType.Keyword,
                    Pattern = "database"
                },
                new PatternTrigger
                {
                    Type = TriggerType.Keyword,
                    Pattern = "storage"
                }
            },
            instructionTemplate: "When selecting a database: 1. Identify requirements 2. Evaluate options",
            confidenceThreshold: 0.75);

        // Act
        var json = SerializationHelper.Serialize(pattern);
        var deserialized = SerializationHelper.Deserialize<ProceduralPattern>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(pattern.Name, deserialized!.Name);
        Assert.Equal(pattern.Description, deserialized.Description);
        Assert.Equal(pattern.Triggers.Length, deserialized.Triggers.Length);
        Assert.Equal(pattern.InstructionTemplate, deserialized.InstructionTemplate);
    }

    [Fact]
    public void OptimizedOptions_IsCaseInsensitive()
    {
        // Arrange
        var json = "{\"UserId\":\"user-123\",\"Content\":\"test\"}";

        // Act
        var result = SerializationHelper.Deserialize<SimpleMessage>(json);

        // Assert - Should deserialize despite different casing
        Assert.NotNull(result);
        Assert.Equal("user-123", result!.UserId);
        Assert.Equal("test", result.Content);
    }

    private class TestClass
    {
        public DateTime Timestamp { get; set; }
    }

    private class SimpleMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
