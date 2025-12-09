using System.IO.Compression;
using System.Text;

namespace MemoryKit.Infrastructure.Compression;

/// <summary>
/// GZip compression implementation for memory storage optimization.
/// Provides good compression ratio with fast compression/decompression speed.
/// Ideal for Redis and general-purpose compression.
/// </summary>
public class GzipCompressionService : ICompressionService
{
    private readonly CompressionLevel _compressionLevel;

    public string AlgorithmName => "GZip";

    public GzipCompressionService(CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        _compressionLevel = compressionLevel;
    }

    public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        using var outputStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(outputStream, _compressionLevel, leaveOpen: true))
        {
            await gzipStream.WriteAsync(data, cancellationToken);
        }

        return outputStream.ToArray();
    }

    public async Task<byte[]> CompressAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<byte>();

        var data = Encoding.UTF8.GetBytes(text);
        return await CompressAsync(data, cancellationToken);
    }

    public async Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        if (compressedData == null || compressedData.Length == 0)
            return Array.Empty<byte>();

        using var inputStream = new MemoryStream(compressedData);
        using var outputStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            await gzipStream.CopyToAsync(outputStream, cancellationToken);
        }

        return outputStream.ToArray();
    }

    public async Task<string> DecompressToStringAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        var decompressed = await DecompressAsync(compressedData, cancellationToken);
        return Encoding.UTF8.GetString(decompressed);
    }
}
