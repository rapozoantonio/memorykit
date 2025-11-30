using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.Azure;

public class AzureTableScratchpadServiceTests
{
    private readonly Mock<TableServiceClient> _mockTableService;
    private readonly Mock<TableClient> _mockTableClient;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<AzureTableScratchpadService>> _mockLogger;

    public AzureTableScratchpadServiceTests()
    {
        _mockTableService = new Mock<TableServiceClient>();
        _mockTableClient = new Mock<TableClient>();
        _mockLogger = new Mock<ILogger<AzureTableScratchpadService>>();

        // Setup configuration using ConfigurationBuilder
        var configValues = new Dictionary<string, string>
        {
            ["MemoryKit:Azure:TableNames:Scratchpad"] = "scratchpad",
            ["MemoryKit:Scratchpad:TtlDays"] = "30"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues!)
            .Build();

        _mockTableService.Setup(ts => ts.GetTableClient(It.IsAny<string>()))
            .Returns(_mockTableClient.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully()
    {
        // Act
        var service = new AzureTableScratchpadService(
            _mockTableService.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task StoreFactsAsync_Should_Call_UpsertEntityAsync()
    {
        // Arrange
        var service = new AzureTableScratchpadService(
            _mockTableService.Object,
            _configuration,
            _mockLogger.Object);

        var facts = new[]
        {
            ExtractedFact.Create("user1", "conv1", "name", "John", EntityType.Person, 0.8)
        };

        _mockTableClient.Setup(tc => tc.UpsertEntityAsync(
            It.IsAny<ITableEntity>(),
            It.IsAny<TableUpdateMode>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await service.StoreFactsAsync("user1", "conv1", facts);

        // Assert
        _mockTableClient.Verify(tc => tc.UpsertEntityAsync(
            It.IsAny<ITableEntity>(),
            It.IsAny<TableUpdateMode>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DeleteUserDataAsync_Initialization_Test()
    {
        // Arrange & Act
        var service = new AzureTableScratchpadService(
            _mockTableService.Object,
            _configuration,
            _mockLogger.Object);

        // Assert - Service should be created successfully
        Assert.NotNull(service);
        
        // Note: Full DeleteUserDataAsync testing requires Azure Table Storage emulator
        // or integration tests with actual Azure resources
    }
}
