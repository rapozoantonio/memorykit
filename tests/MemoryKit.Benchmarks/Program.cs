using BenchmarkDotNet.Running;
using MemoryKit.Benchmarks;

Console.WriteLine("╔═══════════════════════════════════════════════╗");
Console.WriteLine("║     🧠 MemoryKit Benchmark Suite             ║");
Console.WriteLine("╚═══════════════════════════════════════════════╝\n");

Console.WriteLine("Select benchmark suite to run:\n");
Console.WriteLine("1. ⚡ Memory Retrieval Benchmarks (Original)");
Console.WriteLine("2. 📊 Comparative Benchmarks (WITH vs WITHOUT MemoryKit)");
Console.WriteLine("3. 📈 Scalability Benchmarks");
Console.WriteLine("4. 🔄 Concurrency Benchmarks");
Console.WriteLine("5. 🎯 Importance Calculation Benchmarks");
Console.WriteLine("6. 🚀 Run ALL Benchmarks\n");

Console.Write("Enter your choice (1-6): ");
var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        BenchmarkRunner.Run<MemoryRetrievalBenchmarks>();
        break;
    case "2":
        BenchmarkRunner.Run<ComparativeBenchmarks>();
        break;
    case "3":
        BenchmarkRunner.Run<ScalabilityBenchmarks>();
        break;
    case "4":
        BenchmarkRunner.Run<ConcurrencyBenchmarks>();
        break;
    case "5":
        BenchmarkRunner.Run<ImportanceBenchmarks>();
        break;
    case "6":
        Console.WriteLine("\n🏃 Running all benchmark suites...\n");
        BenchmarkRunner.Run<MemoryRetrievalBenchmarks>();
        BenchmarkRunner.Run<ComparativeBenchmarks>();
        BenchmarkRunner.Run<ScalabilityBenchmarks>();
        BenchmarkRunner.Run<ConcurrencyBenchmarks>();
        BenchmarkRunner.Run<ImportanceBenchmarks>();
        break;
    default:
        Console.WriteLine("Invalid choice. Running original benchmarks...");
        BenchmarkRunner.Run<MemoryRetrievalBenchmarks>();
        break;
}

Console.WriteLine("\n✅ Benchmark completed! Check BenchmarkDotNet.Artifacts folder for results.");
Console.WriteLine("Press any key to exit.");
Console.ReadKey();
