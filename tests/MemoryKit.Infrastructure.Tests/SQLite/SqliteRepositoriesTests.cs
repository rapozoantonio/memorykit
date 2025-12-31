using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.SQLite;
using MemoryKit.Infrastructure.SQLite.Repositories;
using MemoryKit.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.SQLite;

/// <summary>
/// Integration tests for SQLite Working Memory Repository.
/// </summary>
public class SqliteWorkingMemoryRepositoryTests : IDisposable
{
    private readonly SqliteMemoryKitDbContext _context;
    private readonly SqliteWorkingMemoryRepository _repository;
    private readonly Mock<ILogger<SqliteWorkingMemoryRepository>> _loggerMock;
    private readonly string _tempDbPath;

    public SqliteWorkingMemoryRepositoryTests()
    {
        // Create a temporary SQLite database for testing
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_memorykit_{Guid.NewGuid()}.db");
        
        var options = new DbContextOptionsBuilder<SqliteMemoryKitDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        _context = new SqliteMemoryKitDbContext(options, _tempDbPath);
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<SqliteWorkingMemoryRepository>>();
        _repository = new SqliteWorkingMemoryRepository(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AddAsync_WithNewMessage_AddsMessageToDatabase()
    {
        // Arrange
        var userId = "test-user-123";
        var conversationId = "test-conv-123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "Test message content");

        // Act
        await _repository.AddAsync(userId, conversationId, message);

        // Assert
        var retrieved = await _repository.GetRecentAsync(userId, conversationId, 10);
        Assert.Single(retrieved);
        Assert.Equal(message.Id, retrieved[0].Id);
        Assert.Equal("Test message content", retrieved[0].Content);
    }

    [Fact]
    public async Task GetRecentAsync_WithMultipleMessages_ReturnsMostRecentInOrder()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        
        var msg1 = Message.Create(userId, conversationId, MessageRole.User, "First message");
        var msg2 = Message.Create(userId, conversationId, MessageRole.Assistant, "Second message");
        var msg3 = Message.Create(userId, conversationId, MessageRole.User, "Third message");

        await _repository.AddAsync(userId, conversationId, msg1);
        await Task.Delay(10);
        await _repository.AddAsync(userId, conversationId, msg2);
        await Task.Delay(10);
        await _repository.AddAsync(userId, conversationId, msg3);

        // Act
        var messages = await _repository.GetRecentAsync(userId, conversationId, 10);

        // Assert
        Assert.Equal(3, messages.Length);
        Assert.Equal("First message", messages[0].Content);
        Assert.Equal("Second message", messages[1].Content);
        Assert.Equal("Third message", messages[2].Content);
    }

    [Fact]
    public async Task RemoveAsync_WithValidMessageId_RemovesMessage()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        var message = Message.Create(userId, conversationId, MessageRole.User, "To be deleted");
        
        await _repository.AddAsync(userId, conversationId, message);

        // Act
        await _repository.RemoveAsync(userId, conversationId, message.Id);

        // Assert
        var retrieved = await _repository.GetRecentAsync(userId, conversationId, 10);
        Assert.Empty(retrieved);
    }

    [Fact]
    public async Task ClearAsync_WithConversation_DeletesAllMessagesForConversation()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        
        await _repository.AddAsync(userId, conversationId, 
            Message.Create(userId, conversationId, MessageRole.User, "Message 1"));
        await _repository.AddAsync(userId, conversationId, 
            Message.Create(userId, conversationId, MessageRole.User, "Message 2"));

        // Act
        await _repository.ClearAsync(userId, conversationId);

        // Assert
        var retrieved = await _repository.GetRecentAsync(userId, conversationId, 10);
        Assert.Empty(retrieved);
    }

    [Fact]
    public async Task DeleteUserDataAsync_RemovesAllUserMessages()
    {
        // Arrange
        var userId = "user-to-delete";
        var conversationId = "conv-123";
        
        await _repository.AddAsync(userId, conversationId, 
            Message.Create(userId, conversationId, MessageRole.User, "User message"));
        
        // Add a message from another user to ensure we don't delete it
        var otherUserId = "other-user";
        await _repository.AddAsync(otherUserId, conversationId,
            Message.Create(otherUserId, conversationId, MessageRole.User, "Other user message"));

        // Act
        await _repository.DeleteUserDataAsync(userId);

        // Assert
        var userMessages = await _repository.GetRecentAsync(userId, conversationId, 10);
        Assert.Empty(userMessages);
        
        var otherUserMessages = await _repository.GetRecentAsync(otherUserId, conversationId, 10);
        Assert.Single(otherUserMessages);
    }

    [Fact]
    public async Task PromoteToSemanticAsync_WithHighImportanceMessages_ReturnsPromotionCount()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        
        var msg1 = Message.Create(userId, conversationId, MessageRole.User, "Important message");
        msg1.MarkAsImportant(0.9); // High importance
        
        await _repository.AddAsync(userId, conversationId, msg1);

        // Act
        var promotedCount = await _repository.PromoteToSemanticAsync(userId);

        // Assert
        Assert.Equal(1, promotedCount);
        var remaining = await _repository.GetRecentAsync(userId, conversationId, 10);
        Assert.Empty(remaining);
    }

    public void Dispose()
    {
        _context?.Dispose();
        
        // Clean up temp database file
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch { }
    }
}

/// <summary>
/// Integration tests for SQLite Semantic Memory Repository.
/// </summary>
public class SqliteSemanticMemoryRepositoryTests : IDisposable
{
    private readonly SqliteMemoryKitDbContext _context;
    private readonly SqliteSemanticMemoryRepository _repository;
    private readonly Mock<ILogger<SqliteSemanticMemoryRepository>> _loggerMock;
    private readonly string _tempDbPath;

    public SqliteSemanticMemoryRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_memorykit_{Guid.NewGuid()}.db");
        
        var options = new DbContextOptionsBuilder<SqliteMemoryKitDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        _context = new SqliteMemoryKitDbContext(options, _tempDbPath);
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<SqliteSemanticMemoryRepository>>();
        _repository = new SqliteSemanticMemoryRepository(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AddAsync_WithFact_StoresFact()
    {
        // Arrange
        var fact = ExtractedFact.Create(
            "user-123",
            "conv-123",
            "fact",
            "The capital of France is Paris",
            EntityType.Location);

        // Act
        var id = await _repository.AddAsync(fact);

        // Assert
        Assert.NotNull(id);
        var retrieved = await _repository.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal("The capital of France is Paris", retrieved.Value);
    }

    [Fact]
    public async Task GetByKeyAsync_WithTextSearch_ReturnsMatchingFacts()
    {
        // Arrange
        var fact1 = ExtractedFact.Create("user-123", "conv-123", "fact", 
            "Python is a programming language", EntityType.Person);
        var fact2 = ExtractedFact.Create("user-123", "conv-123", "fact", 
            "Java is also a programming language", EntityType.Person);

        await _repository.AddAsync(fact1);
        await _repository.AddAsync(fact2);

        // Act
        var results = await _repository.GetByKeyAsync("user-123", "programming");

        // Assert
        Assert.Equal(2, results.Length);
    }

    [Fact]
    public async Task UpdateAsync_WithFact_UpdatesFact()
    {
        // Arrange
        var fact = ExtractedFact.Create("user-123", "conv-123", "fact", 
            "Original content", EntityType.Person);
        var id = await _repository.AddAsync(fact);

        // Update the fact
        fact.Id = id;
        var updatedFact = ExtractedFact.Create("user-123", "conv-123", "fact",
            "Updated content", EntityType.Person);
        updatedFact.Id = id;

        // Act
        await _repository.UpdateAsync(updatedFact);

        // Assert
        var retrieved = await _repository.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated content", retrieved.Value);
    }

    [Fact]
    public async Task DeleteAsync_WithFactId_DeletesFact()
    {
        // Arrange
        var fact = ExtractedFact.Create("user-123", "conv-123", "fact",
            "To be deleted", EntityType.Person);
        var id = await _repository.AddAsync(fact);

        // Act
        await _repository.DeleteAsync(id);

        // Assert
        var retrieved = await _repository.GetByIdAsync(id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteUserDataAsync_RemovesAllUserFacts()
    {
        // Arrange
        var userId = "user-to-delete";
        var otherUserId = "other-user";
        
        var fact1 = ExtractedFact.Create(userId, "conv-123", "fact",
            "User fact", EntityType.Person);
        var fact2 = ExtractedFact.Create(otherUserId, "conv-123", "fact",
            "Other user fact", EntityType.Person);

        await _repository.AddAsync(fact1);
        await _repository.AddAsync(fact2);

        // Act
        await _repository.DeleteUserDataAsync(userId);

        // Assert
        var userFacts = await _repository.GetByUserAsync(userId);
        Assert.Empty(userFacts);
        
        var otherFacts = await _repository.GetByUserAsync(otherUserId);
        Assert.Single(otherFacts);
    }

    [Fact]
    public async Task SearchByEmbeddingAsync_WithSQLite_LogsWarningAndReturnsEmpty()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var results = await _repository.SearchByEmbeddingAsync("user-123", embedding);

        // Assert
        Assert.Empty(results);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Vector search requested in SQLite")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context?.Dispose();
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch { }
    }
}

/// <summary>
/// Integration tests for SQLite Episodic Memory Repository.
/// </summary>
public class SqliteEpisodicMemoryRepositoryTests : IDisposable
{
    private readonly SqliteMemoryKitDbContext _context;
    private readonly SqliteEpisodicMemoryRepository _repository;
    private readonly Mock<ILogger<SqliteEpisodicMemoryRepository>> _loggerMock;
    private readonly string _tempDbPath;

    public SqliteEpisodicMemoryRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_memorykit_{Guid.NewGuid()}.db");
        
        var options = new DbContextOptionsBuilder<SqliteMemoryKitDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        _context = new SqliteMemoryKitDbContext(options, _tempDbPath);
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<SqliteEpisodicMemoryRepository>>();
        _repository = new SqliteEpisodicMemoryRepository(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AddEventAsync_WithEvent_StoresEvent()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        var eventType = "conversation_start";
        var content = "User started a conversation";
        var occurredAt = DateTime.UtcNow;

        // Act
        var eventId = await _repository.AddEventAsync(userId, conversationId, eventType, content, occurredAt);

        // Assert
        Assert.NotNull(eventId);
        var retrieved = await _repository.GetEventByIdAsync(eventId);
        Assert.NotNull(retrieved);
        Assert.Equal(eventType, retrieved.EventType);
    }

    [Fact]
    public async Task GetEventsByTypeAsync_WithEventType_ReturnsMatchingEvents()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        
        await _repository.AddEventAsync(userId, conversationId, "message_sent", "User sent message", DateTime.UtcNow);
        await _repository.AddEventAsync(userId, conversationId, "message_sent", "Another message", DateTime.UtcNow);
        await _repository.AddEventAsync(userId, conversationId, "conversation_end", "Ended", DateTime.UtcNow);

        // Act
        var events = await _repository.GetEventsByTypeAsync(userId, "message_sent");

        // Assert
        Assert.Equal(2, events.Length);
    }

    [Fact]
    public async Task GetEventsByTimeRangeAsync_WithTimeRange_ReturnsEventsInRange()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        var now = DateTime.UtcNow;
        
        await _repository.AddEventAsync(userId, conversationId, "event", "Past event", now.AddHours(-2));
        await _repository.AddEventAsync(userId, conversationId, "event", "Current event", now);
        await _repository.AddEventAsync(userId, conversationId, "event", "Future event", now.AddHours(2));

        // Act
        var events = await _repository.GetEventsByTimeRangeAsync(
            userId, conversationId, 
            now.AddHours(-1), 
            now.AddHours(1));

        // Assert
        Assert.Single(events);
        Assert.Equal("Current event", events[0].Content);
    }

    [Fact]
    public async Task DeleteEventAsync_WithEventId_DeletesEvent()
    {
        // Arrange
        var userId = "user-123";
        var conversationId = "conv-123";
        var eventId = await _repository.AddEventAsync(
            userId, conversationId, "event", "To delete", DateTime.UtcNow);

        // Act
        await _repository.DeleteEventAsync(eventId);

        // Assert
        var retrieved = await _repository.GetEventByIdAsync(eventId);
        Assert.Null(retrieved);
    }

    public void Dispose()
    {
        _context?.Dispose();
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch { }
    }
}

/// <summary>
/// Integration tests for SQLite Procedural Memory Repository.
/// </summary>
public class SqliteProceduralMemoryRepositoryTests : IDisposable
{
    private readonly SqliteMemoryKitDbContext _context;
    private readonly SqliteProceduralMemoryRepository _repository;
    private readonly Mock<ILogger<SqliteProceduralMemoryRepository>> _loggerMock;
    private readonly string _tempDbPath;

    public SqliteProceduralMemoryRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_memorykit_{Guid.NewGuid()}.db");
        
        var options = new DbContextOptionsBuilder<SqliteMemoryKitDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        _context = new SqliteMemoryKitDbContext(options, _tempDbPath);
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<SqliteProceduralMemoryRepository>>();
        _repository = new SqliteProceduralMemoryRepository(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AddPatternAsync_WithPattern_StoresPattern()
    {
        // Arrange
        var userId = "user-123";
        var patternName = "greeting_pattern";
        var triggers = "user says hello";
        var response = "respond with greeting";

        // Act
        var patternId = await _repository.AddPatternAsync(userId, patternName, triggers, response);

        // Assert
        Assert.NotNull(patternId);
        var retrieved = await _repository.GetByIdAsync(patternId);
        Assert.NotNull(retrieved);
        Assert.Equal(patternName, retrieved.PatternName);
    }

    [Fact]
    public async Task RecordSuccessAsync_WithPattern_IncrementsSuccessCount()
    {
        // Arrange
        var userId = "user-123";
        var patternId = await _repository.AddPatternAsync(
            userId, "test_pattern", "trigger", "response");

        // Act
        await _repository.RecordSuccessAsync(patternId);
        await _repository.RecordSuccessAsync(patternId);

        // Assert
        var pattern = await _repository.GetByIdAsync(patternId);
        Assert.NotNull(pattern);
        Assert.Equal(2, pattern.SuccessCount);
    }

    [Fact]
    public async Task RecordFailureAsync_WithPattern_IncrementsFailureCount()
    {
        // Arrange
        var userId = "user-123";
        var patternId = await _repository.AddPatternAsync(
            userId, "test_pattern", "trigger", "response");

        // Act
        await _repository.RecordFailureAsync(patternId);

        // Assert
        var pattern = await _repository.GetByIdAsync(patternId);
        Assert.NotNull(pattern);
        Assert.Equal(1, pattern.FailureCount);
    }

    [Fact]
    public async Task GetByNameAsync_WithPatternName_ReturnsMatchingPatterns()
    {
        // Arrange
        var userId = "user-123";
        var patternName = "greeting";
        
        await _repository.AddPatternAsync(userId, patternName, "trigger1", "response1");
        await _repository.AddPatternAsync(userId, patternName, "trigger2", "response2");
        await _repository.AddPatternAsync(userId, "other", "trigger3", "response3");

        // Act
        var patterns = await _repository.GetByNameAsync(userId, patternName);

        // Assert
        Assert.Equal(2, patterns.Length);
    }

    [Fact]
    public async Task FindByTriggersAsync_WithTriggerText_ReturnsMatchingPatterns()
    {
        // Arrange
        var userId = "user-123";
        
        await _repository.AddPatternAsync(userId, "pattern1", "user says hello", "respond");
        await _repository.AddPatternAsync(userId, "pattern2", "user says goodbye", "respond");
        await _repository.AddPatternAsync(userId, "pattern3", "other trigger", "respond");

        // Act
        var patterns = await _repository.FindByTriggersAsync(userId, "says");

        // Assert
        Assert.Equal(2, patterns.Length);
    }

    public void Dispose()
    {
        _context?.Dispose();
        try
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }
        catch { }
    }
}
