using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.Services;
using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Enums;
using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.InMemory;
using MockService = MemoryKit.Infrastructure.SemanticKernel.MockSemanticKernelService;

namespace MemoryKit.ConsoleDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════╗");
        Console.WriteLine("║     🧠 MemoryKit Interactive Demo            ║");
        Console.WriteLine("║     Real-World AI Application Integration     ║");
        Console.WriteLine("╚═══════════════════════════════════════════════╝\n");

        // Setup DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Get orchestrator
        var orchestrator = serviceProvider.GetRequiredService<IMemoryOrchestrator>();
        var llm = serviceProvider.GetRequiredService<ISemanticKernelService>();

        // Run demo scenarios
        await RunInteractiveDemo(orchestrator, llm);
    }

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information));

        // Register MemoryKit services (in-memory for demo)
        services.AddSingleton<IWorkingMemoryService, InMemoryWorkingMemoryService>();
        services.AddSingleton<IScratchpadService, InMemoryScratchpadService>();
        services.AddSingleton<IEpisodicMemoryService, InMemoryEpisodicMemoryService>();
        
        services.AddSingleton<ISemanticKernelService, MockService>();
        
        services.AddSingleton<IProceduralMemoryService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EnhancedInMemoryProceduralMemoryService>>();
            var sk = sp.GetRequiredService<ISemanticKernelService>();
            return new EnhancedInMemoryProceduralMemoryService(logger, sk);
        });

        services.AddSingleton<IPrefrontalController, PrefrontalController>();
        services.AddSingleton<IAmygdalaImportanceEngine, AmygdalaImportanceEngine>();

        services.AddSingleton<IMemoryOrchestrator, MemoryOrchestrator>();
    }

    static async Task RunInteractiveDemo(IMemoryOrchestrator orchestrator, ISemanticKernelService llm)
    {
        var userId = "demo_user";
        var conversationId = Guid.NewGuid().ToString();

        Console.WriteLine("Select a demo scenario:\n");
        Console.WriteLine("1. 💬 Customer Support Chatbot");
        Console.WriteLine("2. 📚 Educational AI Tutor");
        Console.WriteLine("3. 🛠️  Developer Assistant");
        Console.WriteLine("4. 🎮 Interactive Mode (Chat freely)\n");

        Console.Write("Enter your choice (1-4): ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await RunCustomerSupportDemo(orchestrator, llm, userId, conversationId);
                break;
            case "2":
                await RunEducationalTutorDemo(orchestrator, llm, userId, conversationId);
                break;
            case "3":
                await RunDeveloperAssistantDemo(orchestrator, llm, userId, conversationId);
                break;
            case "4":
                await RunInteractiveMode(orchestrator, llm, userId, conversationId);
                break;
            default:
                Console.WriteLine("Invalid choice. Running Interactive Mode...");
                await RunInteractiveMode(orchestrator, llm, userId, conversationId);
                break;
        }
    }

    static async Task RunCustomerSupportDemo(
        IMemoryOrchestrator orchestrator, 
        ISemanticKernelService llm, 
        string userId, 
        string conversationId)
    {
        Console.WriteLine("\n═══════════════════════════════════════════════");
        Console.WriteLine("📞 SCENARIO: Customer Support Chatbot");
        Console.WriteLine("═══════════════════════════════════════════════\n");
        Console.WriteLine("Use Case: Customer calls multiple times about the same issue.");
        Console.WriteLine("MemoryKit remembers previous interactions across sessions.\n");

        Console.WriteLine("🔹 Day 1 - First Call:");
        await SimulateConversation(orchestrator, userId, conversationId, new[]
        {
            ("Customer", "Hi, my internet isn't working. Order #12345."),
            ("Agent", "I see order #12345. Let me check... Your modem needs a firmware update."),
            ("Customer", "How long will it take?"),
            ("Agent", "About 10 minutes. You'll need to restart your modem after."),
        });

        Console.WriteLine("\n⏳ [Simulating 3 days passing...]\n");
        await Task.Delay(1000);

        Console.WriteLine("🔹 Day 4 - Follow-up Call:");
        await SimulateConversation(orchestrator, userId, conversationId, new[]
        {
            ("Customer", "Hi, I'm still having internet issues."),
        });

        var context = await orchestrator.RetrieveContextAsync(
            userId, conversationId, "internet issues order", CancellationToken.None);

        Console.WriteLine("\n🧠 MemoryKit Context Retrieval:");
        Console.WriteLine($"   ✓ Retrieved {context.WorkingMemory.Length} recent messages");
        Console.WriteLine($"   ✓ Retrieved {context.Facts.Length} extracted facts");
        Console.WriteLine($"   ✓ Retrieval time: {context.RetrievalLatencyMs}ms\n");

        Console.WriteLine("🎯 Value: Agent immediately knows:");
        Console.WriteLine("   • Customer's order number (#12345)");
        Console.WriteLine("   • Previous issue (firmware update)");
        Console.WriteLine("   • Last interaction details\n");

        Console.Write("\nPress Enter to continue...");
        Console.ReadLine();
    }

    static async Task RunEducationalTutorDemo(
        IMemoryOrchestrator orchestrator,
        ISemanticKernelService llm,
        string userId,
        string conversationId)
    {
        Console.WriteLine("\n═══════════════════════════════════════════════");
        Console.WriteLine("📚 SCENARIO: Educational AI Tutor");
        Console.WriteLine("═══════════════════════════════════════════════\n");
        Console.WriteLine("Use Case: Personalized learning with progressive complexity.\n");

        Console.WriteLine("🔹 Week 1 - Introduction to Machine Learning:");
        await SimulateConversation(orchestrator, userId, conversationId, new[]
        {
            ("Student", "I'm new to machine learning. Where should I start?"),
            ("Tutor", "Great! Let's start with supervised learning."),
            ("Student", "What's an example of supervised learning?"),
            ("Tutor", "Email spam detection! The model learns from labeled examples."),
        });

        Console.WriteLine("\n⏳ [Student practices for 1 week...]\n");
        await Task.Delay(1000);

        Console.WriteLine("🔹 Week 2 - Ready for Advanced Topics:");
        var context = await orchestrator.RetrieveContextAsync(
            userId, conversationId, "student progress", CancellationToken.None);

        Console.WriteLine("\n🧠 MemoryKit Learning Analytics:");
        Console.WriteLine("   ✓ Topics covered: supervised learning, classification");
        Console.WriteLine("   ✓ Student level: Beginner → Intermediate");
        Console.WriteLine("   ✓ Recommended next: Neural Networks\n");

        Console.Write("\nPress Enter to continue...");
        Console.ReadLine();
    }

    static async Task RunDeveloperAssistantDemo(
        IMemoryOrchestrator orchestrator,
        ISemanticKernelService llm,
        string userId,
        string conversationId)
    {
        Console.WriteLine("\n═══════════════════════════════════════════════");
        Console.WriteLine("🛠️  SCENARIO: Developer Assistant");
        Console.WriteLine("═══════════════════════════════════════════════\n");
        Console.WriteLine("Use Case: Coding assistant that remembers project context.\n");

        Console.WriteLine("🔹 Day 1 - Project Setup:");
        await SimulateConversation(orchestrator, userId, conversationId, new[]
        {
            ("Dev", "Starting a new web API project. Using ASP.NET Core 9.0 with PostgreSQL."),
            ("Assistant", "Great choice! I'll remember that."),
            ("Dev", "Let's use Clean Architecture."),
            ("Assistant", "Perfect. I'll suggest patterns aligned with Clean Architecture."),
        });

        Console.WriteLine("\n⏳ [Developer works on features for 2 days...]\n");
        await Task.Delay(1000);

        Console.WriteLine("🔹 Day 3 - Configuration Question:");
        var context = await orchestrator.RetrieveContextAsync(
            userId, conversationId, "database connection", CancellationToken.None);

        Console.WriteLine("\n🧠 MemoryKit Project Context:");
        Console.WriteLine("   ✓ Framework: ASP.NET Core 9.0");
        Console.WriteLine("   ✓ Database: PostgreSQL");
        Console.WriteLine("   ✓ Architecture: Clean Architecture");
        Console.WriteLine($"   ✓ Retrieved in: {context.RetrievalLatencyMs}ms\n");

        Console.WriteLine("🎯 Developer Productivity Gains:");
        Console.WriteLine("   ✓ No context switching to find configs");
        Console.WriteLine("   ✓ Consistent architecture suggestions");
        Console.WriteLine("   ✓ Code suggestions match project style\n");

        Console.Write("\nPress Enter to continue...");
        Console.ReadLine();
    }

    static async Task RunInteractiveMode(
        IMemoryOrchestrator orchestrator,
        ISemanticKernelService llm,
        string userId,
        string conversationId)
    {
        Console.WriteLine("\n═══════════════════════════════════════════════");
        Console.WriteLine("🎮 INTERACTIVE MODE");
        Console.WriteLine("═══════════════════════════════════════════════\n");
        Console.WriteLine("Chat with MemoryKit-powered assistant.");
        Console.WriteLine("Commands: 'stats' | 'context' | 'exit'\n");

        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.ToLower() == "exit") break;

            if (input.ToLower() == "stats")
            {
                await ShowMemoryStats(orchestrator, userId, conversationId);
                continue;
            }

            if (input.ToLower() == "context")
            {
                await ShowCurrentContext(orchestrator, userId, conversationId, input);
                continue;
            }

            // Store user message
            var userMessage = Message.Create(userId, conversationId, MessageRole.User, input);
            await orchestrator.StoreAsync(userId, conversationId, userMessage, CancellationToken.None);

            // Retrieve context and generate response
            var context = await orchestrator.RetrieveContextAsync(
                userId, conversationId, input, CancellationToken.None);

            var response = await llm.AnswerWithContextAsync(input, context, CancellationToken.None);

            // Store assistant response
            var assistantMessage = Message.Create(userId, conversationId, MessageRole.Assistant, response);
            await orchestrator.StoreAsync(userId, conversationId, assistantMessage, CancellationToken.None);

            Console.WriteLine($"\nAssistant: {response}");
            Console.WriteLine($"[Importance: {userMessage.Metadata.ImportanceScore:F2} | Latency: {context.RetrievalLatencyMs}ms]\n");
        }
    }

    static async Task SimulateConversation(
        IMemoryOrchestrator orchestrator,
        string userId,
        string conversationId,
        (string role, string content)[] exchanges)
    {
        foreach (var (role, content) in exchanges)
        {
            var messageRole = role.ToLower().Contains("customer") || 
                            role.ToLower().Contains("student") || 
                            role.ToLower().Contains("dev")
                ? MessageRole.User
                : MessageRole.Assistant;

            var message = Message.Create(userId, conversationId, messageRole, content);
            await orchestrator.StoreAsync(userId, conversationId, message, CancellationToken.None);

            Console.WriteLine($"   {role}: {content}");
            await Task.Delay(300);
        }
    }

    static async Task ShowMemoryStats(IMemoryOrchestrator orchestrator, string userId, string conversationId)
    {
        var context = await orchestrator.RetrieveContextAsync(
            userId, conversationId, "stats", CancellationToken.None);

        Console.WriteLine("\n📊 Memory Statistics:");
        Console.WriteLine($"   Working Memory (L3): {context.WorkingMemory.Length} messages");
        Console.WriteLine($"   Facts (L2): {context.Facts.Length} facts");
        Console.WriteLine($"   Episodic (L1): {context.ArchivedMessages.Length} archived messages");
        Console.WriteLine($"   Total Tokens: {context.TotalTokens}");
        Console.WriteLine($"   Last Retrieval: {context.RetrievalLatencyMs}ms\n");
    }

    static async Task ShowCurrentContext(
        IMemoryOrchestrator orchestrator,
        string userId,
        string conversationId,
        string query)
    {
        var context = await orchestrator.RetrieveContextAsync(
            userId, conversationId, query, CancellationToken.None);

        Console.WriteLine("\n🧠 Current Context:");
        Console.WriteLine(context.ToPromptContext());
        Console.WriteLine();
    }
}
