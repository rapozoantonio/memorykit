using MemoryKit.ClientSDK;

// ══════════════════════════════════════════════════════════════
// MemoryKit Client SDK Usage Examples
// ══════════════════════════════════════════════════════════════

Console.WriteLine("╔═══════════════════════════════════════════════╗");
Console.WriteLine("║     🧠 MemoryKit Client SDK Examples         ║");
Console.WriteLine("╚═══════════════════════════════════════════════╝");
Console.WriteLine();

// Initialize the client
var apiUrl = "https://localhost:5001"; // Update with your API URL
using var client = new MemoryKitClient(apiUrl);

try
{
    // ═══ Example 1: Create Conversation ═══
    Console.WriteLine("📝 Example 1: Creating a new conversation...");
    var conversationId = Guid.NewGuid().ToString();
    var userId = "demo-user";
    
    var conversation = await client.CreateConversationAsync(
        conversationId,
        userId,
        metadata: "Demo conversation from SDK");
    
    Console.WriteLine($"✓ Created conversation: {conversation.ConversationId}");
    Console.WriteLine();

    // ═══ Example 2: Add Messages ═══
    Console.WriteLine("💬 Example 2: Adding messages...");
    
    var messages = new[]
    {
        "Hello! I need help with my Azure subscription.",
        "I'm having trouble with billing.",
        "My subscription ID is SUB-12345.",
        "The issue started last week."
    };

    foreach (var msg in messages)
    {
        var response = await client.AddMessageAsync(
            conversationId,
            userId,
            msg,
            isUserMessage: true);
        
        Console.WriteLine($"  → Message: {msg}");
        Console.WriteLine($"    Importance: {response.CalculatedImportance:F2}");
    }
    Console.WriteLine();

    // ═══ Example 3: Query Context ═══
    Console.WriteLine("🔍 Example 3: Querying conversation context...");
    
    var query = "What is the user's subscription ID?";
    var context = await client.QueryContextAsync(
        conversationId,
        userId,
        query,
        topK: 5);

    Console.WriteLine($"Query: {query}");
    Console.WriteLine($"Found {context.RelevantMemories.Count} relevant memories:");
    
    foreach (var memory in context.RelevantMemories)
    {
        Console.WriteLine($"  • [{memory.Timestamp:HH:mm:ss}] {memory.Content}");
        Console.WriteLine($"    Relevance: {memory.RelevanceScore:F2} | Importance: {memory.Importance:F2}");
    }
    Console.WriteLine();

    // ═══ Example 4: Batch Operations ═══
    Console.WriteLine("📦 Example 4: Batch message processing...");
    
    var batchMessages = new[]
    {
        "Can you check the payment status?",
        "I also need to update my payment method.",
        "Please send me an invoice."
    };

    var batchResults = await client.AddMessagesBatchAsync(
        conversationId,
        userId,
        batchMessages);

    Console.WriteLine($"✓ Processed {batchResults.Count} messages in batch");
    Console.WriteLine($"  Average importance: {batchResults.Average(r => r.CalculatedImportance):F2}");
    Console.WriteLine();

    // ═══ Example 5: Performance Metrics ═══
    Console.WriteLine("📊 Example 5: Retrieving performance metrics...");
    
    var metrics = await client.GetPerformanceMetricsAsync(windowMinutes: 5);
    
    Console.WriteLine($"Performance (last 5 minutes):");
    Console.WriteLine($"  Total Operations: {metrics.TotalOperations}");
    Console.WriteLine($"  Avg Latency: {metrics.AverageLatencyMs:F2}ms");
    Console.WriteLine($"  P95 Latency: {metrics.P95LatencyMs:F2}ms");
    Console.WriteLine($"  Ops/Second: {metrics.OperationsPerSecond:F1}");
    
    if (metrics.OperationBreakdown.Any())
    {
        Console.WriteLine("  Operation Breakdown:");
        foreach (var (operation, stats) in metrics.OperationBreakdown)
        {
            Console.WriteLine($"    {operation}: {stats.Count} ops, {stats.AverageMs:F2}ms avg");
        }
    }
    Console.WriteLine();

    // ═══ Example 6: Health Check ═══
    Console.WriteLine("🏥 Example 6: Checking API health...");
    
    var health = await client.GetHealthAsync();
    
    Console.WriteLine($"Status: {health.Status}");
    Console.WriteLine($"Message: {health.Message}");
    Console.WriteLine($"P95 Latency: {health.P95LatencyMs:F2}ms");
    Console.WriteLine();

    // ═══ Example 7: Custom Metrics ═══
    Console.WriteLine("📈 Example 7: Recording custom metric...");
    
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    // Simulate some operation
    await Task.Delay(50);
    stopwatch.Stop();

    await client.RecordMetricAsync(
        "CustomOperation",
        stopwatch.Elapsed.TotalMilliseconds);
    
    Console.WriteLine($"✓ Recorded custom metric: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
    Console.WriteLine();

    // ═══ Example 8: Conversation Retrieval ═══
    Console.WriteLine("📖 Example 8: Retrieving conversation details...");
    
    var retrieved = await client.GetConversationAsync(conversationId);
    
    Console.WriteLine($"Conversation ID: {retrieved.ConversationId}");
    Console.WriteLine($"User ID: {retrieved.UserId}");
    Console.WriteLine($"Created At: {retrieved.CreatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Metadata: {retrieved.Metadata}");
    Console.WriteLine();

    Console.WriteLine("✅ All examples completed successfully!");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"❌ HTTP Error: {ex.Message}");
    Console.WriteLine("   Make sure the MemoryKit API is running.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
