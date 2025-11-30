using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.Azure;
using MemoryKit.Infrastructure.InMemory;
using Moq;
using StackExchange.Redis;
using System.Collections.Generic;

namespace MemoryKit.Benchmarks;

/// <summary>
/// Benchmarks comparing InMemory vs Azure storage providers.
/// Note: Requires Azure resources to run Azure benchmarks.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StorageProviderBenchmarks
{
    private InMemoryWorkingMemoryService? _inMemoryService;
    private AzureRedisWorkingMemoryService? _azureService;
    private Message? _testMessage;
    private IConfiguration? _configuration;

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Setup InMemory service
        _inMemoryService = new InMemoryWorkingMemoryService(
            loggerFactory.CreateLogger<InMemoryWorkingMemoryService>());

        // Setup Configuration
        var configValues = new Dictionary<string, string>
        {
            ["MemoryKit:WorkingMemory:MaxItems"] = "10",
            ["MemoryKit:WorkingMemory:TtlHours"] = "24",
            ["MemoryKit:Azure:RedisConnectionString"] = "localhost:6379"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues!)
            .Build();

        // Setup Azure service with mock (for benchmark structure - won't actually run without real Redis)
        try
        {
            var mockRedis = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            mockRedis.Setup(r => r.GetDatabase(-1, null)).Returns(mockDatabase.Object);
            
            mockDatabase.Setup(db => db.SortedSetAddAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            _azureService = new AzureRedisWorkingMemoryService(
                mockRedis.Object,
                _configuration,
                loggerFactory.CreateLogger<AzureRedisWorkingMemoryService>());
        }
        catch
        {
            // Azure service setup failed - benchmarks will skip Azure tests
        }

        // Create test message
        _testMessage = Message.Create("bench_user", "bench_conv", MessageRole.User, "Test message for benchmarking");
    }

    [Benchmark(Baseline = true, Description = "InMemory - Add Message")]
    public async Task InMemory_AddMessage()
    {
        if (_inMemoryService == null || _testMessage == null) return;
        await _inMemoryService.AddAsync("bench_user", "bench_conv", _testMessage);
    }

    [Benchmark(Description = "InMemory - Get Recent (10 messages)")]
    public async Task InMemory_GetRecent()
    {
        if (_inMemoryService == null) return;
        await _inMemoryService.GetRecentAsync("bench_user", "bench_conv", 10);
    }

    // Note: Azure benchmarks require actual Azure Redis connection
    // Uncomment when running with real Azure resources
    /*
    [Benchmark(Description = "Azure Redis - Add Message")]
    public async Task Azure_AddMessage()
    {
        if (_azureService == null || _testMessage == null) return;
        await _azureService.AddAsync("bench_user", "bench_conv", _testMessage);
    }

    [Benchmark(Description = "Azure Redis - Get Recent (10 messages)")]
    public async Task Azure_GetRecent()
    {
        if (_azureService == null) return;
        await _azureService.GetRecentAsync("bench_user", "bench_conv", 10);
    }
    */
}
