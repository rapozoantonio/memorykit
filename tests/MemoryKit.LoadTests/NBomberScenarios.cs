using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace MemoryKit.LoadTests;

/// <summary>
/// NBomber load tests for MemoryKit API
/// Simulates realistic production load with multiple concurrent users
/// </summary>
public class MemoryKitLoadTests
{
    private const string ApiBaseUrl = "https://localhost:5001"; // Update with your API URL
    private static readonly Random Random = new();

    public static void Main()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════╗");
        Console.WriteLine("║   🚀 MemoryKit Load Testing with NBomber     ║");
        Console.WriteLine("╚═══════════════════════════════════════════════╝");
        Console.WriteLine();

        ShowMenu();
    }

    private static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("Select a load test scenario:");
            Console.WriteLine("1. 🔥 Smoke Test (5 users, 1 minute)");
            Console.WriteLine("2. ⚡ Moderate Load (25 users, 3 minutes)");
            Console.WriteLine("3. 💪 Heavy Load (100 users, 5 minutes)");
            Console.WriteLine("4. 🌊 Stress Test (200 users, 10 minutes)");
            Console.WriteLine("5. 📊 Mixed Workload (varied operations)");
            Console.WriteLine("6. 🎯 Run All Tests");
            Console.WriteLine("7. Exit");
            Console.WriteLine();
            Console.Write("Enter your choice (1-7): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    RunSmokeTest();
                    break;
                case "2":
                    RunModerateLoadTest();
                    break;
                case "3":
                    RunHeavyLoadTest();
                    break;
                case "4":
                    RunStressTest();
                    break;
                case "5":
                    RunMixedWorkloadTest();
                    break;
                case "6":
                    RunAllTests();
                    break;
                case "7":
                    Console.WriteLine("👋 Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
            ShowMenu();
        }
    }

    #region Test Scenarios

    private static void RunSmokeTest()
    {
        Console.WriteLine("🔥 Running Smoke Test...");
        Console.WriteLine("   Target: 5 concurrent users");
        Console.WriteLine("   Duration: 1 minute");
        Console.WriteLine();

        var scenario = CreateConversationScenario("SmokeTest", copies: 5);
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpClientFactory())
            .Run();
    }

    private static void RunModerateLoadTest()
    {
        Console.WriteLine("⚡ Running Moderate Load Test...");
        Console.WriteLine("   Target: 25 concurrent users");
        Console.WriteLine("   Duration: 3 minutes");
        Console.WriteLine();

        var scenario = CreateConversationScenario("ModerateLoad", copies: 25);
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpClientFactory())
            .Run();
    }

    private static void RunHeavyLoadTest()
    {
        Console.WriteLine("💪 Running Heavy Load Test...");
        Console.WriteLine("   Target: 100 concurrent users");
        Console.WriteLine("   Duration: 5 minutes");
        Console.WriteLine();

        var scenario = CreateConversationScenario("HeavyLoad", copies: 100);
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpClientFactory())
            .Run();
    }

    private static void RunStressTest()
    {
        Console.WriteLine("🌊 Running Stress Test...");
        Console.WriteLine("   Target: 200 concurrent users");
        Console.WriteLine("   Duration: 10 minutes");
        Console.WriteLine();

        var scenario = CreateConversationScenario("StressTest", copies: 200);
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpClientFactory())
            .Run();
    }

    private static void RunMixedWorkloadTest()
    {
        Console.WriteLine("📊 Running Mixed Workload Test...");
        Console.WriteLine("   Combines: Create, Message, Query, Metrics");
        Console.WriteLine("   Duration: 5 minutes");
        Console.WriteLine();

        var createScenario = CreateConversationScenario("CreateConversation", copies: 20);
        var messageScenario = CreateMessageScenario("AddMessage", copies: 50);
        var queryScenario = CreateQueryScenario("QueryContext", copies: 30);
        var metricsScenario = CreateMetricsScenario("GetMetrics", copies: 10);

        NBomberRunner
            .RegisterScenarios(createScenario, messageScenario, queryScenario, metricsScenario)
            .WithWorkerPlugins(new HttpClientFactory())
            .Run();
    }

    private static void RunAllTests()
    {
        Console.WriteLine("🎯 Running All Test Suites...");
        Console.WriteLine();

        Console.WriteLine("=== 1/4: Smoke Test ===");
        RunSmokeTest();

        Console.WriteLine();
        Console.WriteLine("=== 2/4: Moderate Load ===");
        RunModerateLoadTest();

        Console.WriteLine();
        Console.WriteLine("=== 3/4: Heavy Load ===");
        RunHeavyLoadTest();

        Console.WriteLine();
        Console.WriteLine("=== 4/4: Mixed Workload ===");
        RunMixedWorkloadTest();

        Console.WriteLine();
        Console.WriteLine("✅ All test suites completed!");
    }

    #endregion

    #region Scenario Builders

    private static ScenarioProps CreateConversationScenario(string name, int copies)
    {
        var step = Step.Create($"{name}_CreateConversation", async context =>
        {
            var conversationId = Guid.NewGuid().ToString();
            var userId = $"user-{Random.Next(1000)}";

            var request = new
            {
                conversationId,
                userId,
                metadata = "Load test conversation"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var client = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            var response = await client.PostAsync("/api/v1/conversations", content);

            return response.IsSuccessStatusCode
                ? Response.Ok(statusCode: (int)response.StatusCode)
                : Response.Fail(statusCode: (int)response.StatusCode);
        });

        return ScenarioBuilder
            .CreateScenario(name, step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies, TimeSpan.FromMinutes(1))
            );
    }

    private static ScenarioProps CreateMessageScenario(string name, int copies)
    {
        var messages = new[]
        {
            "I need help with my account",
            "What are your business hours?",
            "Can you help me with billing?",
            "I'd like to upgrade my subscription",
            "How do I reset my password?",
            "Tell me about your premium features",
            "I'm having trouble logging in",
            "What payment methods do you accept?"
        };

        var step = Step.Create($"{name}_AddMessage", async context =>
        {
            var conversationId = $"conv-{Random.Next(100)}";
            var userId = $"user-{Random.Next(1000)}";
            var content = messages[Random.Next(messages.Length)];

            var request = new
            {
                conversationId,
                userId,
                content,
                isUserMessage = true
            };

            var httpContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var client = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            var response = await client.PostAsync("/api/v1/messages", httpContent);

            return response.IsSuccessStatusCode
                ? Response.Ok(statusCode: (int)response.StatusCode)
                : Response.Fail(statusCode: (int)response.StatusCode);
        });

        return ScenarioBuilder
            .CreateScenario(name, step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies, TimeSpan.FromMinutes(1))
            );
    }

    private static ScenarioProps CreateQueryScenario(string name, int copies)
    {
        var queries = new[]
        {
            "What did we discuss about billing?",
            "Show me recent conversations",
            "What are the main topics?",
            "Tell me about subscription details",
            "What issues were reported?"
        };

        var step = Step.Create($"{name}_QueryContext", async context =>
        {
            var conversationId = $"conv-{Random.Next(100)}";
            var userId = $"user-{Random.Next(1000)}";
            var query = queries[Random.Next(queries.Length)];

            var request = new
            {
                conversationId,
                userId,
                query,
                topK = 5
            };

            var httpContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var client = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            var response = await client.PostAsync("/api/v1/context/query", httpContent);

            return response.IsSuccessStatusCode
                ? Response.Ok(statusCode: (int)response.StatusCode)
                : Response.Fail(statusCode: (int)response.StatusCode);
        });

        return ScenarioBuilder
            .CreateScenario(name, step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies, TimeSpan.FromMinutes(1))
            );
    }

    private static ScenarioProps CreateMetricsScenario(string name, int copies)
    {
        var step = Step.Create($"{name}_GetMetrics", async context =>
        {
            using var client = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            var response = await client.GetAsync("/api/v1/metrics/performance?windowMinutes=5");

            return response.IsSuccessStatusCode
                ? Response.Ok(statusCode: (int)response.StatusCode)
                : Response.Fail(statusCode: (int)response.StatusCode);
        });

        return ScenarioBuilder
            .CreateScenario(name, step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.KeepConstant(copies, TimeSpan.FromMinutes(1))
            );
    }

    #endregion
}
