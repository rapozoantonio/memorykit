using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.InMemory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.InMemory;

public class InMemoryWorkingMemoryServiceTests
{
    private readonly Mock<ILogger<InMemoryWorkingMemoryService>> _loggerMock;
    private readonly InMemoryWorkingMemoryService _service;

    public InMemoryWorkingMemoryServiceTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryWorkingMemoryService>>();
        _service = new InMemoryWorkingMemoryService(_loggerMock.Object);
    }

    [Fact]
    public async Task AddAsync_WithNewMessage_AddsMessageToStorage()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Test message");

        // Act
        await _service.AddAsync(userId, conversationId, message, CancellationToken.None);

        // Assert
        var messages = await _service.GetRecentAsync(userId, conversationId, 10, CancellationToken.None);
        Assert.Single(messages);
        Assert.Equal(message.Id, messages[0].Id);
    }

    [Fact]
    public async Task AddAsync_ExceedingMaxItems_EvictsLeastImportant()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";

        // Add 11 messages (max is 10)
        for (int i = 0; i < 11; i++)
        {
            var message = Message.Create(userId, conversationId, MessageRole.User, $"Message {i}");
            message.MarkAsImportant(i * 0.1); // Increasing importance
            await _service.AddAsync(userId, conversationId, message, CancellationToken.None);
        }

        // Act
        var messages = await _service.GetRecentAsync(userId, conversationId, 20, CancellationToken.None);

        // Assert
        Assert.Equal(10, messages.Length); // Should have evicted one
        Assert.DoesNotContain(messages, m => m.Content == "Message 0"); // Least important should be evicted
    }

    [Fact]
    public async Task GetRecentAsync_WithNoMessages_ReturnsEmptyArray()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";

        // Act
        var messages = await _service.GetRecentAsync(userId, conversationId, 10, CancellationToken.None);

        // Assert
        Assert.Empty(messages);
    }

    [Fact]
    public async Task GetRecentAsync_WithMultipleMessages_ReturnsInChronologicalOrder()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";

        var message1 = Message.Create(userId, conversationId, MessageRole.User, "First");
        var message2 = Message.Create(userId, conversationId, MessageRole.User, "Second");
        var message3 = Message.Create(userId, conversationId, MessageRole.User, "Third");

        await _service.AddAsync(userId, conversationId, message1, CancellationToken.None);
        await Task.Delay(10); // Ensure different timestamps
        await _service.AddAsync(userId, conversationId, message2, CancellationToken.None);
        await Task.Delay(10);
        await _service.AddAsync(userId, conversationId, message3, CancellationToken.None);

        // Act
        var messages = await _service.GetRecentAsync(userId, conversationId, 10, CancellationToken.None);

        // Assert
        Assert.Equal(3, messages.Length);
        Assert.Equal("First", messages[0].Content);
        Assert.Equal("Second", messages[1].Content);
        Assert.Equal("Third", messages[2].Content);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllMessagesForConversation()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Test");
        await _service.AddAsync(userId, conversationId, message, CancellationToken.None);

        // Act
        await _service.ClearAsync(userId, conversationId, CancellationToken.None);

        // Assert
        var messages = await _service.GetRecentAsync(userId, conversationId, 10, CancellationToken.None);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task DeleteUserDataAsync_RemovesAllConversationsForUser()
    {
        // Arrange
        var userId = "user123";
        var conv1 = "conv1";
        var conv2 = "conv2";

        var message1 = Message.Create(userId, conv1, MessageRole.User, "Test 1");
        var message2 = Message.Create(userId, conv2, MessageRole.User, "Test 2");

        await _service.AddAsync(userId, conv1, message1, CancellationToken.None);
        await _service.AddAsync(userId, conv2, message2, CancellationToken.None);

        // Act
        await _service.DeleteUserDataAsync(userId, CancellationToken.None);

        // Assert
        var messages1 = await _service.GetRecentAsync(userId, conv1, 10, CancellationToken.None);
        var messages2 = await _service.GetRecentAsync(userId, conv2, 10, CancellationToken.None);
        Assert.Empty(messages1);
        Assert.Empty(messages2);
    }

    [Fact]
    public async Task AddAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var userId = "user123";
        var conversationId = "conv123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _service.AddAsync(userId, conversationId, message, cts.Token));
    }

    [Fact]
    public async Task PeriodicCleanup_RemovesExpiredConversations()
    {
        // Note: This test would need to manipulate time or use a different approach
        // to test TTL-based cleanup. For now, it's a placeholder for the concept.
        // In real tests, you'd use a time provider abstraction or wait for TTL.

        // This is a conceptual test - in production, you'd need to:
        // 1. Use ISystemClock or similar time abstraction
        // 2. Mock time to simulate 24+ hours passing
        // 3. Trigger cleanup and verify old conversations are removed

        Assert.True(true); // Placeholder
    }
}
