using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MemoryKit.Infrastructure.Compression;

namespace MemoryKit.Benchmarks.Compression;

/// <summary>
/// Benchmark comparing compression algorithms and their impact on storage.
/// Run with: dotnet run --project tests/MemoryKit.Benchmarks -c Release
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CompressionBenchmark
{
    private byte[] _smallMessage = null!;
    private byte[] _mediumMessage = null!;
    private byte[] _largeMessage = null!;
    
    private GzipCompressionService _gzipService = null!;
    private BrotliCompressionService _brotliService = null!;
    private SelectiveCompressionService _selectiveGzipService = null!;
    private SelectiveCompressionService _selectiveBrotliService = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small message (100 bytes) - typical chat message
        var smallText = "Hello! How can I help you today? This is a short message.";
        _smallMessage = Encoding.UTF8.GetBytes(smallText);

        // Medium message (1KB) - typical assistant response
        var mediumText = string.Join(" ", Enumerable.Repeat(
            "This is a medium-sized message that represents a typical assistant response. " +
            "It contains several sentences with some repetitive patterns that should compress well.",
            10));
        _mediumMessage = Encoding.UTF8.GetBytes(mediumText);

        // Large message (10KB) - detailed conversation or code
        var largeText = string.Join(" ", Enumerable.Repeat(
            "This is a large message that simulates a detailed conversation or code snippet. " +
            "It has lots of repetitive content that will benefit significantly from compression. " +
            "The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet.",
            100));
        _largeMessage = Encoding.UTF8.GetBytes(largeText);

        // Initialize services
        _gzipService = new GzipCompressionService();
        _brotliService = new BrotliCompressionService();
        _selectiveGzipService = new SelectiveCompressionService(_gzipService, 1024);
        _selectiveBrotliService = new SelectiveCompressionService(_brotliService, 1024);
    }

    // ========== Small Message (100 bytes) ==========

    [Benchmark]
    [BenchmarkCategory("Small")]
    public async Task<byte[]> SmallMessage_NoCompression()
    {
        return await Task.FromResult(_smallMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Small")]
    public async Task<byte[]> SmallMessage_GZip()
    {
        return await _gzipService.CompressAsync(_smallMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Small")]
    public async Task<byte[]> SmallMessage_Brotli()
    {
        return await _brotliService.CompressAsync(_smallMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Small")]
    public async Task<byte[]> SmallMessage_SelectiveGZip()
    {
        return await _selectiveGzipService.CompressAsync(_smallMessage);
    }

    // ========== Medium Message (1KB) ==========

    [Benchmark]
    [BenchmarkCategory("Medium")]
    public async Task<byte[]> MediumMessage_NoCompression()
    {
        return await Task.FromResult(_mediumMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Medium")]
    public async Task<byte[]> MediumMessage_GZip()
    {
        return await _gzipService.CompressAsync(_mediumMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Medium")]
    public async Task<byte[]> MediumMessage_Brotli()
    {
        return await _brotliService.CompressAsync(_mediumMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Medium")]
    public async Task<byte[]> MediumMessage_SelectiveGZip()
    {
        return await _selectiveGzipService.CompressAsync(_mediumMessage);
    }

    // ========== Large Message (10KB) ==========

    [Benchmark]
    [BenchmarkCategory("Large")]
    public async Task<byte[]> LargeMessage_NoCompression()
    {
        return await Task.FromResult(_largeMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Large")]
    public async Task<byte[]> LargeMessage_GZip()
    {
        return await _gzipService.CompressAsync(_largeMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Large")]
    public async Task<byte[]> LargeMessage_Brotli()
    {
        return await _brotliService.CompressAsync(_largeMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Large")]
    public async Task<byte[]> LargeMessage_SelectiveGZip()
    {
        return await _selectiveGzipService.CompressAsync(_largeMessage);
    }

    [Benchmark]
    [BenchmarkCategory("Large")]
    public async Task<byte[]> LargeMessage_SelectiveBrotli()
    {
        return await _selectiveBrotliService.CompressAsync(_largeMessage);
    }

    // ========== Round-trip Benchmark ==========

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public async Task<string> LargeMessage_GZip_RoundTrip()
    {
        var compressed = await _gzipService.CompressAsync(_largeMessage);
        return await _gzipService.DecompressToStringAsync(compressed);
    }

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public async Task<string> LargeMessage_Brotli_RoundTrip()
    {
        var compressed = await _brotliService.CompressAsync(_largeMessage);
        return await _brotliService.DecompressToStringAsync(compressed);
    }
}

/// <summary>
/// Benchmark comparing compression ratios achieved for different data types.
/// </summary>
[MemoryDiagnoser]
public class CompressionRatioBenchmark
{
    [Params(100, 500, 1000, 5000, 10000)]
    public int MessageSizeBytes { get; set; }

    private byte[] _testData = null!;
    private GzipCompressionService _gzipService = null!;
    private BrotliCompressionService _brotliService = null!;

    [GlobalSetup]
    public void Setup()
    {
        var text = string.Join(" ", Enumerable.Repeat(
            "The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet.",
            MessageSizeBytes / 50));
        _testData = Encoding.UTF8.GetBytes(text.Substring(0, Math.Min(text.Length, MessageSizeBytes)));

        _gzipService = new GzipCompressionService();
        _brotliService = new BrotliCompressionService();
    }

    [Benchmark(Baseline = true)]
    public int Original_Size()
    {
        return _testData.Length;
    }

    [Benchmark]
    public async Task<int> GZip_CompressedSize()
    {
        var compressed = await _gzipService.CompressAsync(_testData);
        return compressed.Length;
    }

    [Benchmark]
    public async Task<int> Brotli_CompressedSize()
    {
        var compressed = await _brotliService.CompressAsync(_testData);
        return compressed.Length;
    }
}
