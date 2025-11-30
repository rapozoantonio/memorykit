using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.Azure;

public class AzureRedisWorkingMemoryServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<AzureRedisWorkingMemoryService>> _mockLogger;
    private readonly AzureRedisWorkingMemoryService _service;

    public AzureRedisWorkingMemoryServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<AzureRedisWorkingMemoryService>>();

        // Setup Redis mock - handle both parameterless and parameterized GetDatabase calls
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);
        _mockRedis.Setup(r => r.GetDatabase(-1, null))
            .Returns(_mockDatabase.Object);

        // Setup configuration using ConfigurationBuilder
        var configValues = new Dictionary<string, string>
        {
            ["MemoryKit:WorkingMemory:MaxItems"] = "10",
            ["MemoryKit:WorkingMemory:TtlHours"] = "24"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues!)
            .Build();

        _service = new AzureRedisWorkingMemoryService(
            _mockRedis.Object,
            _configuration,
            _mockLogger.Object);
    }

    [Fact]
    public async Task AddAsync_Should_Store_Message_In_Redis()
    {
        // Arrange
        var message = Message.Create("user1", "conv1", MessageRole.User, "Hello");
        
        _mockDatabase.Setup(db => db.SortedSetAddAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<double>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase.Setup(db => db.SortedSetRemoveRangeByRankAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<long>(),
            It.IsAny<long>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(0);

        _mockDatabase.Setup(db => db.KeyExpireAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var exception = await Record.ExceptionAsync(async () =>
            await _service.AddAsync("user1", "conv1", message));

        // Assert - operation should complete without throwing
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetRecentAsync_Should_Return_Empty_Array_When_No_Messages()
    {
        // Arrange
        _mockDatabase.Setup(db => db.SortedSetRangeByRankAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<long>(),
            It.IsAny<long>(),
            It.IsAny<Order>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<RedisValue>());

        // Act
        var result = await _service.GetRecentAsync("user1", "conv1", 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ClearAsync_Should_Delete_Conversation_Key()
    {
        // Arrange
        _mockDatabase.Setup(db => db.KeyDeleteAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _service.ClearAsync("user1", "conv1");

        // Assert
        _mockDatabase.Verify(db => db.KeyDeleteAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
}
