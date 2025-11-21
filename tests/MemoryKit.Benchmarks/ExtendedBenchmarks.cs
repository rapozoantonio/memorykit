using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Infrastructure.InMemory;
using SemanticKernelService = MemoryKit.Infrastructure.SemanticKernel.MockSemanticKernelService;

namespace MemoryKit.Benchmarks;

/// <summary>
/// Extended benchmarks covering edge cases and scaling scenarios.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ScalabilityBenchmarks
{
    private MemoryOrchestrator? _orchestrator;
    private string _userId = "bench_user";
    private string _convId = "bench_conv";

    // Test different conversation sizes
    [Params(10, 50, 100, 500)]
    public int ConversationSize;

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(b => 
            b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var workingMemory = new InMemoryWorkingMemoryService(
            loggerFactory.CreateLogger<InMemoryWorkingMemoryService>());
        var scratchpad = new InMemoryScratchpadService(
            loggerFactory.CreateLogger<InMemoryScratchpadService>());
        var episodic = new InMemoryEpisodicMemoryService(
            loggerFactory.CreateLogger<InMemoryEpisodicMemoryService>());
        var llm = new SemanticKernelService(
            loggerFactory.CreateLogger<SemanticKernelService>());
        var procedural = new EnhancedInMemoryProceduralMemoryService(
            loggerFactory.CreateLogger<EnhancedInMemoryProceduralMemoryService>(), llm);
        var prefrontal = new PrefrontalController(
            loggerFactory.CreateLogger<PrefrontalController>());
        var amygdala = new AmygdalaImportanceEngine(
            loggerFactory.CreateLogger<AmygdalaImportanceEngine>());

        _orchestrator = new MemoryOrchestrator(
            workingMemory, scratchpad, episodic, procedural, prefrontal, amygdala,
            loggerFactory.CreateLogger<MemoryOrchestrator>());

        SeedDataAsync(ConversationSize).Wait();
    }

    private async Task SeedDataAsync(int messageCount)
    {
        for (int i = 0; i < messageCount; i++)
        {
            var msg = Message.Create(_userId, _convId, MessageRole.User, 
                $"Message {i}: This is test content with some keywords like ML, AI, data.");
            await _orchestrator!.StoreAsync(_userId, _convId, msg, CancellationToken.None);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Scalability")]
    public async Task RetrieveContext_ScalingTest()
    {
        await _orchestrator!.RetrieveContextAsync(
            _userId, _convId, "tell me about ML", CancellationToken.None);
    }

    [Benchmark]
    [BenchmarkCategory("Scalability")]
    public async Task StoreMessage_ScalingTest()
    {
        var msg = Message.Create(_userId, _convId, MessageRole.User, "New message");
        await _orchestrator!.StoreAsync(_userId, _convId, msg, CancellationToken.None);
    }
}

/// <summary>
/// Concurrent access benchmarks - simulating multiple users.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ConcurrencyBenchmarks
{
    private MemoryOrchestrator? _orchestrator;
    private readonly List<(string userId, string convId)> _users = new();

    [Params(1, 10, 50)]
    public int ConcurrentUsers;

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(b => 
            b.AddConsole().SetMinimumLevel(LogLevel.Error));
        
        var workingMemory = new InMemoryWorkingMemoryService(
            loggerFactory.CreateLogger<InMemoryWorkingMemoryService>());
        var scratchpad = new InMemoryScratchpadService(
            loggerFactory.CreateLogger<InMemoryScratchpadService>());
        var episodic = new InMemoryEpisodicMemoryService(
            loggerFactory.CreateLogger<InMemoryEpisodicMemoryService>());
        var llm = new SemanticKernelService(
            loggerFactory.CreateLogger<SemanticKernelService>());
        var procedural = new EnhancedInMemoryProceduralMemoryService(
            loggerFactory.CreateLogger<EnhancedInMemoryProceduralMemoryService>(), llm);
        var prefrontal = new PrefrontalController(
            loggerFactory.CreateLogger<PrefrontalController>());
        var amygdala = new AmygdalaImportanceEngine(
            loggerFactory.CreateLogger<AmygdalaImportanceEngine>());

        _orchestrator = new MemoryOrchestrator(
            workingMemory, scratchpad, episodic, procedural, prefrontal, amygdala,
            loggerFactory.CreateLogger<MemoryOrchestrator>());

        // Create N users
        for (int i = 0; i < ConcurrentUsers; i++)
        {
            _users.Add(($"user_{i}", $"conv_{i}"));
        }
    }

    [Benchmark]
    [BenchmarkCategory("Concurrency")]
    public async Task ConcurrentRetrieve()
    {
        var tasks = _users.Select(user =>
            _orchestrator!.RetrieveContextAsync(user.userId, user.convId, 
                "test query", CancellationToken.None));
        
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [BenchmarkCategory("Concurrency")]
    public async Task ConcurrentStore()
    {
        var tasks = _users.Select(user =>
        {
            var msg = Message.Create(user.userId, user.convId, MessageRole.User, "test");
            return _orchestrator!.StoreAsync(user.userId, user.convId, msg, CancellationToken.None);
        });
        
        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Importance calculation benchmarks - core MemoryKit feature.
/// </summary>
[MemoryDiagnoser]
public class ImportanceBenchmarks
{
    private AmygdalaImportanceEngine? _amygdala;

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(b => 
            b.AddConsole().SetMinimumLevel(LogLevel.Error));
        _amygdala = new AmygdalaImportanceEngine(
            loggerFactory.CreateLogger<AmygdalaImportanceEngine>());
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Importance")]
    public async Task CalculateImportance_SimpleMessage()
    {
        var msg = Message.Create("u", "c", MessageRole.User, "Hello");
        await _amygdala!.CalculateImportanceAsync(msg, CancellationToken.None);
    }

    [Benchmark]
    [BenchmarkCategory("Importance")]
    public async Task CalculateImportance_Question()
    {
        var msg = Message.Create("u", "c", MessageRole.User, "What is machine learning?");
        msg.MarkAsQuestion();
        await _amygdala!.CalculateImportanceAsync(msg, CancellationToken.None);
    }

    [Benchmark]
    [BenchmarkCategory("Importance")]
    public async Task CalculateImportance_Decision()
    {
        var msg = Message.Create("u", "c", MessageRole.User,
            "IMPORTANT: From now on, always use Python 3.11. I decided this is critical.");
        msg.MarkAsDecision();
        await _amygdala!.CalculateImportanceAsync(msg, CancellationToken.None);
    }

    [Benchmark]
    [BenchmarkCategory("Importance")]
    public async Task CalculateImportance_WithCode()
    {
        var msg = Message.Create("u", "c", MessageRole.Assistant,
            "```csharp\npublic class Example { public int Id { get; set; } }\n```");
        msg.MarkAsContainingCode();
        await _amygdala!.CalculateImportanceAsync(msg, CancellationToken.None);
    }

    [Benchmark]
    [BenchmarkCategory("Importance")]
    public async Task CalculateImportance_LongMessage()
    {
        var longContent = string.Join(" ", Enumerable.Repeat("detailed explanation", 100));
        var msg = Message.Create("u", "c", MessageRole.Assistant, longContent);
        await _amygdala!.CalculateImportanceAsync(msg, CancellationToken.None);
    }
}
