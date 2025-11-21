using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.InMemory;
using SemanticKernelService = MemoryKit.Infrastructure.SemanticKernel.MockSemanticKernelService;

namespace MemoryKit.Benchmarks;

/// <summary>
/// Comparative benchmarks: WITH MemoryKit vs WITHOUT MemoryKit.
/// Demonstrates the value proposition of hierarchical memory management.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ComparativeBenchmarks
{
    // WITH MemoryKit
    private MemoryOrchestrator? _orchestrator;
    private ISemanticKernelService? _llm;
    
    // WITHOUT MemoryKit (baseline)
    private List<Message> _simpleMessageList = new();
    
    private string _testUserId = string.Empty;
    private string _testConversationId = string.Empty;
    private const int ConversationLength = 50;

    [GlobalSetup]
    public void Setup()
    {
        _testUserId = "benchmark_user";
        _testConversationId = "benchmark_conv";

        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Error));
        
        // Setup MemoryKit components
        var workingMemory = new InMemoryWorkingMemoryService(
            loggerFactory.CreateLogger<InMemoryWorkingMemoryService>());
        var scratchpad = new InMemoryScratchpadService(
            loggerFactory.CreateLogger<InMemoryScratchpadService>());
        var episodic = new InMemoryEpisodicMemoryService(
            loggerFactory.CreateLogger<InMemoryEpisodicMemoryService>());
        
        _llm = new SemanticKernelService(
            loggerFactory.CreateLogger<SemanticKernelService>());
        
        var procedural = new EnhancedInMemoryProceduralMemoryService(
            loggerFactory.CreateLogger<EnhancedInMemoryProceduralMemoryService>(),
            _llm);
        var prefrontal = new PrefrontalController(
            loggerFactory.CreateLogger<PrefrontalController>());
        var amygdala = new AmygdalaImportanceEngine(
            loggerFactory.CreateLogger<AmygdalaImportanceEngine>());

        _orchestrator = new MemoryOrchestrator(
            workingMemory, scratchpad, episodic, procedural, prefrontal, amygdala,
            loggerFactory.CreateLogger<MemoryOrchestrator>());

        // Seed both systems with same conversation data
        SeedConversationData().Wait();
    }

    private async Task SeedConversationData()
    {
        var conversationMessages = new[]
        {
            "Hi, I'm working on a machine learning project",
            "I need to use TensorFlow 2.0",
            "The dataset has 10,000 samples",
            "I'm using Python 3.11",
            "The model architecture is a CNN with 5 layers",
            "First layer has 32 filters",
            "Second layer has 64 filters",
            "I'm using ReLU activation",
            "Training with Adam optimizer",
            "Learning rate is 0.001",
            "Batch size is 32",
            "Training for 50 epochs",
            "Validation split is 20%",
            "Using data augmentation",
            "Horizontal flip and rotation",
            "Current accuracy is 87%",
            "Loss is 0.3",
            "Overfitting after epoch 30",
            "Added dropout layers",
            "Dropout rate is 0.5",
            "Accuracy improved to 89%",
            "Now testing on holdout set",
            "Test accuracy is 88%",
            "Model is performing well",
            "Need to deploy to production",
            "Will use Docker for deployment",
            "API will use FastAPI",
            "Hosting on AWS",
            "Using EC2 t3.large instance",
            "Need to add monitoring",
            "Using CloudWatch for logs",
            "Setting up auto-scaling",
            "Min 2 instances, max 10",
            "Load balancer configured",
            "SSL certificate added",
            "Domain is ml-api.example.com",
            "Rate limiting at 100 req/min",
            "Authentication via API keys",
            "Database is PostgreSQL",
            "Storing predictions in database",
            "Added caching with Redis",
            "Cache TTL is 1 hour",
            "API response time under 100ms",
            "Monitoring with Prometheus",
            "Grafana dashboard created",
            "Alerting on error rate > 5%",
            "Backup schedule is daily",
            "Retention period is 30 days",
            "Disaster recovery plan ready",
            "System went live yesterday"
        };

        // Store in MemoryKit (WITH memory management)
        for (int i = 0; i < ConversationLength && i < conversationMessages.Length; i++)
        {
            var message = Message.Create(
                _testUserId,
                _testConversationId,
                i % 2 == 0 ? MessageRole.User : MessageRole.Assistant,
                conversationMessages[i]);

            await _orchestrator!.StoreAsync(_testUserId, _testConversationId, message, CancellationToken.None);
            
            // Also store in simple list (WITHOUT memory management)
            _simpleMessageList.Add(message);
        }
    }

    // ============================================================================
    // SCENARIO 1: Recent Context Retrieval (Last 10 messages)
    // ============================================================================

    [Benchmark]
    [BenchmarkCategory("RecentContext")]
    public async Task<int> WithMemoryKit_RecentContext()
    {
        // MemoryKit automatically retrieves optimally from Working Memory (L3)
        var context = await _orchestrator!.RetrieveContextAsync(
            _testUserId,
            _testConversationId,
            "continue our discussion",
            CancellationToken.None);

        return context.WorkingMemory.Length;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("RecentContext")]
    public Task<int> WithoutMemoryKit_RecentContext()
    {
        // Naive approach: Take last N messages (no intelligence)
        var recentMessages = _simpleMessageList
            .OrderByDescending(m => m.Timestamp)
            .Take(10)
            .ToArray();

        return Task.FromResult(recentMessages.Length);
    }

    // ============================================================================
    // SCENARIO 2: Fact Retrieval (Find specific information)
    // ============================================================================

    [Benchmark]
    [BenchmarkCategory("FactRetrieval")]
    public async Task<string> WithMemoryKit_FindFact()
    {
        // MemoryKit uses semantic search across indexed facts
        var context = await _orchestrator!.RetrieveContextAsync(
            _testUserId,
            _testConversationId,
            "What was the learning rate?",
            CancellationToken.None);

        return context.Facts.FirstOrDefault()?.Value ?? "not found";
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FactRetrieval")]
    public Task<string> WithoutMemoryKit_FindFact()
    {
        // Naive approach: Linear search through all messages
        var relevantMessage = _simpleMessageList
            .FirstOrDefault(m => m.Content.Contains("learning rate", StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(relevantMessage?.Content ?? "not found");
    }

    // ============================================================================
    // SCENARIO 3: Deep Recall (Find old information from many messages ago)
    // ============================================================================

    [Benchmark]
    [BenchmarkCategory("DeepRecall")]
    public async Task<int> WithMemoryKit_DeepRecall()
    {
        // MemoryKit searches across all layers including episodic memory
        var context = await _orchestrator!.RetrieveContextAsync(
            _testUserId,
            _testConversationId,
            "Quote me exactly what was said about the CNN architecture",
            CancellationToken.None);

        return context.ArchivedMessages.Length + context.WorkingMemory.Length;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DeepRecall")]
    public Task<int> WithoutMemoryKit_DeepRecall()
    {
        // Naive approach: Search through ALL messages (expensive!)
        var relevantMessages = _simpleMessageList
            .Where(m => m.Content.Contains("CNN", StringComparison.OrdinalIgnoreCase) ||
                       m.Content.Contains("layer", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(relevantMessages.Length);
    }

    // ============================================================================
    // SCENARIO 4: Context Assembly (Prepare for LLM prompt)
    // ============================================================================

    [Benchmark]
    [BenchmarkCategory("ContextAssembly")]
    public async Task<int> WithMemoryKit_AssembleContext()
    {
        // MemoryKit intelligently assembles context from multiple layers
        var context = await _orchestrator!.RetrieveContextAsync(
            _testUserId,
            _testConversationId,
            "What's the current status of deployment?",
            CancellationToken.None);

        var promptContext = context.ToPromptContext();
        return promptContext.Length;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ContextAssembly")]
    public async Task<int> WithoutMemoryKit_AssembleContext()
    {
        // Naive approach: Concatenate all messages (token limit risk!)
        var allContent = string.Join("\n\n", _simpleMessageList.Select(m => 
            $"[{m.Role}]: {m.Content}"));

        // Simulate LLM call
        if (_llm != null)
        {
            await _llm.ClassifyQueryAsync("What's the current status?", CancellationToken.None);
        }

        return allContent.Length;
    }

    // ============================================================================
    // SCENARIO 5: Storage Efficiency (Memory overhead)
    // ============================================================================

    [Benchmark]
    [BenchmarkCategory("Storage")]
    public async Task WithMemoryKit_StoreMessage()
    {
        var message = Message.Create(
            _testUserId,
            _testConversationId,
            MessageRole.User,
            "This is a new message to store");

        // MemoryKit calculates importance, extracts entities, manages eviction
        await _orchestrator!.StoreAsync(_testUserId, _testConversationId, message, CancellationToken.None);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Storage")]
    public Task WithoutMemoryKit_StoreMessage()
    {
        var message = Message.Create(
            _testUserId,
            _testConversationId,
            MessageRole.User,
            "This is a new message to store");

        // Naive approach: Just append to list (no intelligence)
        _simpleMessageList.Add(message);
        
        return Task.CompletedTask;
    }

    // ============================================================================
    // SCENARIO 6: Query Classification (Understanding user intent)
    // ============================================================================

    [Benchmark]
    [BenchmarkCategory("QueryClassification")]
    public async Task<string> WithMemoryKit_ClassifyQuery()
    {
        // MemoryKit uses PrefrontalController for intelligent classification
        var plan = await _orchestrator!.BuildQueryPlanAsync(
            "What was the batch size I mentioned earlier?",
            new ConversationState
            {
                UserId = _testUserId,
                ConversationId = _testConversationId,
                MessageCount = ConversationLength,
                TurnCount = ConversationLength / 2,
                ElapsedTime = TimeSpan.FromMinutes(30),
                QueryCount = 10,
                LastQueryTime = DateTime.UtcNow.AddMinutes(-5),
                AverageResponseTimeMs = 50,
                LastActivity = DateTime.UtcNow
            },
            CancellationToken.None);

        return plan.Type.ToString();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("QueryClassification")]
    public Task<string> WithoutMemoryKit_ClassifyQuery()
    {
        // Naive approach: Simple pattern matching (limited intelligence)
        var query = "What was the batch size I mentioned earlier?";
        
        if (query.Contains("what", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("FactRetrieval");
        
        if (query.Contains("continue", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("Continuation");
        
        return Task.FromResult("Unknown");
    }
}
