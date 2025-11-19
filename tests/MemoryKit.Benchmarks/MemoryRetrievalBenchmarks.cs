using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.ValueObjects;
using MemoryKit.Infrastructure.InMemory;
using MemoryKit.Infrastructure.SemanticKernel;

namespace MemoryKit.Benchmarks;

/// <summary>
/// Performance benchmarks for memory retrieval operations.
/// Target: Sub-150ms query response time (as per TRD Section 11.1)
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MemoryRetrievalBenchmarks
{
    private MemoryOrchestrator? _orchestrator;
    private string _testUserId = string.Empty;
    private string _testConversationId = string.Empty;
    private ILogger<MemoryOrchestrator>? _logger;

    [GlobalSetup]
    public void Setup()
    {
        _testUserId = "benchmark_user";
        _testConversationId = "benchmark_conv";

        // Setup services
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MemoryOrchestrator>();

        var workingMemory = new InMemoryWorkingMemoryService(
            loggerFactory.CreateLogger<InMemoryWorkingMemoryService>());

        var scratchpad = new InMemoryScratchpadService(
            loggerFactory.CreateLogger<InMemoryScratchpadService>());

        var episodic = new InMemoryEpisodicMemoryService(
            loggerFactory.CreateLogger<InMemoryEpisodicMemoryService>());

        var semanticKernel = new MockSemanticKernelService(
            loggerFactory.CreateLogger<MockSemanticKernelService>());

        var procedural = new EnhancedInMemoryProceduralMemoryService(
            loggerFactory.CreateLogger<EnhancedInMemoryProceduralMemoryService>(),
            semanticKernel);

        var prefrontal = new PrefrontalController(
            loggerFactory.CreateLogger<PrefrontalController>());

        var amygdala = new AmygdalaImportanceEngine(
            loggerFactory.CreateLogger<AmygdalaImportanceEngine>());

        _orchestrator = new MemoryOrchestrator(
            workingMemory,
            scratchpad,
            episodic,
            procedural,
            prefrontal,
            amygdala,
            _logger);

        // Pre-populate with test data
        SeedTestData(workingMemory, scratchpad, episodic).Wait();
    }

    private async Task SeedTestData(
        InMemoryWorkingMemoryService workingMemory,
        InMemoryScratchpadService scratchpad,
        InMemoryEpisodicMemoryService episodic)
    {
        // Add 10 messages to working memory
        for (int i = 0; i < 10; i++)
        {
            var message = Message.Create(
                _testUserId,
                _testConversationId,
                MessageRole.User,
                $"Test message {i}: This is a sample message for benchmarking.");

            await workingMemory.AddAsync(_testUserId, _testConversationId, message);
            await episodic.ArchiveAsync(message);
        }

        // Add 50 facts to scratchpad
        var facts = new ExtractedFact[50];
        for (int i = 0; i < 50; i++)
        {
            var fact = ExtractedFact.Create(
                _testUserId,
                _testConversationId,
                $"TestFact_{i}",
                $"Test value {i}",
                EntityType.Other,
                0.5 + (i * 0.01));

            fact.SetEmbedding(GenerateRandomEmbedding());
            facts[i] = fact;
        }

        await scratchpad.StoreFactsAsync(_testUserId, _testConversationId, facts);
    }

    private float[] GenerateRandomEmbedding(int dimensions = 1536)
    {
        var random = new Random(42); // Fixed seed for consistency
        var embedding = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            embedding[i] = (float)random.NextDouble();
        }
        return embedding;
    }

    /// <summary>
    /// Benchmark: Working Memory Only (Continuation queries)
    /// Expected: Sub-5ms (as per TRD Section 11.1)
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task WorkingMemoryOnly_Continuation()
    {
        if (_orchestrator == null) throw new InvalidOperationException("Orchestrator not initialized");

        await _orchestrator.RetrieveContextAsync(
            _testUserId,
            _testConversationId,
            "continue",
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark: Working Memory + Scratchpad (Fact retrieval)
    /// Expected: Sub-30ms (as per TRD Section 11.1)
    /// </summary>
    [Benchmark]
    public async Task WorkingMemoryPlusScratchpad_FactRetrieval()
    {
        if (_orchestrator == null) throw new InvalidOperationException("Orchestrator not initialized");

        await _orchestrator.RetrieveContextAsync(
            _testUserId,
            _testConversationId,
            "what was TestFact_10?",
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark: All layers (Deep recall)
    /// Expected: Sub-150ms (as per TRD Section 11.1)
    /// </summary>
    [Benchmark]
    public async Task AllLayers_DeepRecall()
    {
        if (_orchestrator == null) throw new InvalidOperationException("Orchestrator not initialized");

        await _orchestrator.RetrieveContextAsync(
            _testUserId,
            _testConversationId,
            "quote me exactly what was said about TestFact",
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark: Message storage with importance calculation
    /// </summary>
    [Benchmark]
    public async Task StoreMessage_WithImportance()
    {
        if (_orchestrator == null) throw new InvalidOperationException("Orchestrator not initialized");

        var message = Message.Create(
            _testUserId,
            _testConversationId,
            MessageRole.User,
            "This is a very important message that should be remembered!");

        await _orchestrator.StoreAsync(
            _testUserId,
            _testConversationId,
            message,
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark: Query plan building
    /// </summary>
    [Benchmark]
    public async Task BuildQueryPlan()
    {
        if (_orchestrator == null) throw new InvalidOperationException("Orchestrator not initialized");

        await _orchestrator.BuildQueryPlanAsync(
            "what is the current status of the project?",
            new ConversationState
            {
                ConversationId = _testConversationId,
                MessageCount = 10,
                LastActivity = DateTime.UtcNow
            },
            CancellationToken.None);
    }
}
