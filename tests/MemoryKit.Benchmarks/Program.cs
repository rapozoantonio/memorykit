using BenchmarkDotNet.Running;
using MemoryKit.Benchmarks;

// Run all benchmarks
var summary = BenchmarkRunner.Run<MemoryRetrievalBenchmarks>();

Console.WriteLine("Benchmark completed. Press any key to exit.");
Console.ReadKey();
