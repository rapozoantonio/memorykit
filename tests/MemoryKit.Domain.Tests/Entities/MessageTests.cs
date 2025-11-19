using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using Xunit;

namespace MemoryKit.Domain.Tests.Entities;

public class MessageTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsMessage()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var role = MessageRole.User;
        var content = "Hello, world!";

        // Act
        var message = Message.Create(userId, conversationId, role, content);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(userId, message.UserId);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(role, message.Role);
        Assert.Equal(content, message.Content);
        Assert.NotEmpty(message.Id);
        Assert.True(message.Timestamp <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, "conv123", "content")]
    [InlineData("", "conv123", "content")]
    [InlineData("   ", "conv123", "content")]
    public void Create_WithInvalidUserId_ThrowsArgumentException(string? userId, string conversationId, string content)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Message.Create(userId!, conversationId, MessageRole.User, content));
        Assert.Contains("User ID", exception.Message);
    }

    [Theory]
    [InlineData("user123", null, "content")]
    [InlineData("user123", "", "content")]
    [InlineData("user123", "   ", "content")]
    public void Create_WithInvalidConversationId_ThrowsArgumentException(string userId, string? conversationId, string content)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Message.Create(userId, conversationId!, MessageRole.User, content));
        Assert.Contains("Conversation ID", exception.Message);
    }

    [Theory]
    [InlineData("user123", "conv123", null)]
    [InlineData("user123", "conv123", "")]
    [InlineData("user123", "conv123", "   ")]
    public void Create_WithInvalidContent_ThrowsArgumentException(string userId, string conversationId, string? content)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Message.Create(userId, conversationId, MessageRole.User, content!));
        Assert.Contains("Content", exception.Message);
    }

    [Fact]
    public void MarkAsImportant_SetsImportanceScore()
    {
        // Arrange
        var message = Message.Create("user123", "conv123", MessageRole.User, "Test");
        var score = 0.85;

        // Act
        message.MarkAsImportant(score);

        // Assert
        Assert.Equal(score, message.Metadata.ImportanceScore);
    }

    [Theory]
    [InlineData(-0.1, 0.0)]
    [InlineData(1.1, 1.0)]
    [InlineData(0.5, 0.5)]
    public void MarkAsImportant_ClampsScore(double input, double expected)
    {
        // Arrange
        var message = Message.Create("user123", "conv123", MessageRole.User, "Test");

        // Act
        message.MarkAsImportant(input);

        // Assert
        Assert.Equal(expected, message.Metadata.ImportanceScore);
    }
}
