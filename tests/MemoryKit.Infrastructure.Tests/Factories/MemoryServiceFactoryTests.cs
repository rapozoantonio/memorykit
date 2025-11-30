using System;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.Factories;
using MemoryKit.Infrastructure.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.Factories;

public class MemoryServiceFactoryTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<MemoryServiceFactory>> _mockLogger;
    private readonly ServiceCollection _services;
    private readonly IServiceProvider _serviceProvider;

    public MemoryServiceFactoryTests()
    {
        _mockLogger = new Mock<ILogger<MemoryServiceFactory>>();
        _services = new ServiceCollection();

        // Register InMemory services
        _services.AddSingleton<InMemoryWorkingMemoryService>();
        _services.AddSingleton<InMemoryScratchpadService>();
        _services.AddSingleton<InMemoryEpisodicMemoryService>();
        _services.AddSingleton(Mock.Of<ILogger<InMemoryWorkingMemoryService>>());
        _services.AddSingleton(Mock.Of<ILogger<InMemoryScratchpadService>>());
        _services.AddSingleton(Mock.Of<ILogger<InMemoryEpisodicMemoryService>>());
        _services.AddSingleton(Mock.Of<ILogger<EnhancedInMemoryProceduralMemoryService>>());
        _services.AddSingleton(Mock.Of<ISemanticKernelService>());
        _services.AddSingleton<EnhancedInMemoryProceduralMemoryService>();

        _serviceProvider = _services.BuildServiceProvider();
        
        // Initialize mock configuration - will be set per test
        _mockConfiguration = new Mock<IConfiguration>();
    }
    
    private void SetupConfiguration(string providerValue)
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(providerValue);
        _mockConfiguration.Setup(c => c.GetSection("MemoryKit:StorageProvider"))
            .Returns(mockSection.Object);
    }

    [Fact]
    public void CreateWorkingMemoryService_WithInMemoryProvider_ReturnsInMemoryService()
    {
        // Arrange
        SetupConfiguration("InMemory");

        var factory = new MemoryServiceFactory(
            _serviceProvider,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var service = factory.CreateWorkingMemoryService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InMemoryWorkingMemoryService>(service);
    }

    [Fact]
    public void CreateScratchpadService_WithInMemoryProvider_ReturnsInMemoryService()
    {
        // Arrange
        SetupConfiguration("InMemory");

        var factory = new MemoryServiceFactory(
            _serviceProvider,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var service = factory.CreateScratchpadService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InMemoryScratchpadService>(service);
    }

    [Fact]
    public void CreateProceduralMemoryService_WithInMemoryProvider_ReturnsEnhancedService()
    {
        // Arrange
        SetupConfiguration("InMemory");

        var factory = new MemoryServiceFactory(
            _serviceProvider,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var service = factory.CreateProceduralMemoryService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<EnhancedInMemoryProceduralMemoryService>(service);
    }

    [Fact]
    public void CreateEpisodicMemoryService_WithInMemoryProvider_ReturnsInMemoryService()
    {
        // Arrange
        SetupConfiguration("InMemory");

        var factory = new MemoryServiceFactory(
            _serviceProvider,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var service = factory.CreateEpisodicMemoryService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InMemoryEpisodicMemoryService>(service);
    }

    [Fact]
    public void CreateWorkingMemoryService_WithInvalidProvider_DefaultsToInMemory()
    {
        // Arrange
        SetupConfiguration("InvalidProvider");

        var factory = new MemoryServiceFactory(
            _serviceProvider,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var service = factory.CreateWorkingMemoryService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InMemoryWorkingMemoryService>(service);
    }
}
