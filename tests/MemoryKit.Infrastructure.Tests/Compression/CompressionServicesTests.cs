using System.Text;
using MemoryKit.Infrastructure.Compression;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.Compression;

public class CompressionServicesTests
{
    private const string SampleText = "This is a test message that should compress well. " +
                                     "Compression works best with repetitive text patterns. " +
                                     "The quick brown fox jumps over the lazy dog. " +
                                     "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";

    [Fact]
    public async Task GzipCompressionService_CompressAndDecompress_RoundTripsSuccessfully()
    {
        // Arrange
        var service = new GzipCompressionService();
        var data = Encoding.UTF8.GetBytes(SampleText);

        // Act
        var compressed = await service.CompressAsync(data);
        var decompressed = await service.DecompressAsync(compressed);
        var result = Encoding.UTF8.GetString(decompressed);

        // Assert
        Assert.Equal(SampleText, result);
        Assert.True(compressed.Length < data.Length, "Compressed data should be smaller");
    }

    [Fact]
    public async Task GzipCompressionService_CompressString_RoundTripsSuccessfully()
    {
        // Arrange
        var service = new GzipCompressionService();

        // Act
        var compressed = await service.CompressAsync(SampleText);
        var decompressed = await service.DecompressToStringAsync(compressed);

        // Assert
        Assert.Equal(SampleText, decompressed);
    }

    [Fact]
    public async Task BrotliCompressionService_CompressAndDecompress_RoundTripsSuccessfully()
    {
        // Arrange
        var service = new BrotliCompressionService();
        var data = Encoding.UTF8.GetBytes(SampleText);

        // Act
        var compressed = await service.CompressAsync(data);
        var decompressed = await service.DecompressAsync(compressed);
        var result = Encoding.UTF8.GetString(decompressed);

        // Assert
        Assert.Equal(SampleText, result);
        Assert.True(compressed.Length < data.Length, "Compressed data should be smaller");
    }

    [Fact]
    public async Task BrotliCompressionService_CompressString_RoundTripsSuccessfully()
    {
        // Arrange
        var service = new BrotliCompressionService();

        // Act
        var compressed = await service.CompressAsync(SampleText);
        var decompressed = await service.DecompressToStringAsync(compressed);

        // Assert
        Assert.Equal(SampleText, decompressed);
    }

    [Fact]
    public async Task SelectiveCompressionService_SmallData_DoesNotCompress()
    {
        // Arrange
        var innerService = new GzipCompressionService();
        var service = new SelectiveCompressionService(innerService, compressionThresholdBytes: 1024);
        var smallText = "Small message";
        var data = Encoding.UTF8.GetBytes(smallText);

        // Act
        var result = await service.CompressAsync(data);
        var decompressed = await service.DecompressAsync(result);
        var resultText = Encoding.UTF8.GetString(decompressed);

        // Assert
        Assert.Equal(smallText, resultText);
        // Result should be only slightly larger than original (1 byte marker)
        Assert.True(result.Length <= data.Length + 10);
    }

    [Fact]
    public async Task SelectiveCompressionService_LargeData_Compresses()
    {
        // Arrange
        var innerService = new GzipCompressionService();
        var service = new SelectiveCompressionService(innerService, compressionThresholdBytes: 100);
        
        // Create large repetitive text
        var largeText = string.Join(" ", Enumerable.Repeat(SampleText, 20));
        var data = Encoding.UTF8.GetBytes(largeText);

        // Act
        var result = await service.CompressAsync(data);
        var decompressed = await service.DecompressAsync(result);
        var resultText = Encoding.UTF8.GetString(decompressed);

        // Assert
        Assert.Equal(largeText, resultText);
        Assert.True(result.Length < data.Length, "Large data should be compressed");
    }

    [Fact]
    public async Task SelectiveCompressionService_CompressString_RoundTripsSuccessfully()
    {
        // Arrange
        var innerService = new GzipCompressionService();
        var service = new SelectiveCompressionService(innerService);
        var largeText = string.Join(" ", Enumerable.Repeat(SampleText, 20));

        // Act
        var compressed = await service.CompressAsync(largeText);
        var decompressed = await service.DecompressToStringAsync(compressed);

        // Assert
        Assert.Equal(largeText, decompressed);
    }

    [Fact]
    public void CompressionServices_HaveCorrectAlgorithmNames()
    {
        // Arrange & Act
        var gzip = new GzipCompressionService();
        var brotli = new BrotliCompressionService();
        var selective = new SelectiveCompressionService(gzip);

        // Assert
        Assert.Equal("GZip", gzip.AlgorithmName);
        Assert.Equal("Brotli", brotli.AlgorithmName);
        Assert.Equal("Selective-GZip", selective.AlgorithmName);
    }

    [Fact]
    public async Task GzipCompressionService_EmptyData_HandlesGracefully()
    {
        // Arrange
        var service = new GzipCompressionService();

        // Act
        var compressed = await service.CompressAsync(Array.Empty<byte>());
        var decompressed = await service.DecompressAsync(compressed);

        // Assert
        Assert.Empty(compressed);
        Assert.Empty(decompressed);
    }

    [Fact]
    public async Task BrotliCompressionService_EmptyString_HandlesGracefully()
    {
        // Arrange
        var service = new BrotliCompressionService();

        // Act
        var compressed = await service.CompressAsync(string.Empty);
        var decompressed = await service.DecompressToStringAsync(compressed);

        // Assert
        Assert.Empty(compressed);
        Assert.Empty(decompressed);
    }

    [Theory]
    [InlineData(System.IO.Compression.CompressionLevel.Fastest)]
    [InlineData(System.IO.Compression.CompressionLevel.Optimal)]
    [InlineData(System.IO.Compression.CompressionLevel.SmallestSize)]
    public async Task GzipCompressionService_DifferentCompressionLevels_Work(
        System.IO.Compression.CompressionLevel level)
    {
        // Arrange
        var service = new GzipCompressionService(level);
        var largeText = string.Join(" ", Enumerable.Repeat(SampleText, 10));

        // Act
        var compressed = await service.CompressAsync(largeText);
        var decompressed = await service.DecompressToStringAsync(compressed);

        // Assert
        Assert.Equal(largeText, decompressed);
    }
}
