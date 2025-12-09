namespace MemoryKit.Infrastructure.Compression;

/// <summary>
/// Selective compression service that only compresses data above a threshold size.
/// This prevents overhead on small messages while providing compression for larger ones.
/// </summary>
public class SelectiveCompressionService : ICompressionService
{
    private readonly ICompressionService _innerCompressor;
    private readonly int _compressionThresholdBytes;

    // Marker byte to indicate if data is compressed (0xC0 = compressed, 0x00 = uncompressed)
    private const byte CompressedMarker = 0xC0;
    private const byte UncompressedMarker = 0x00;

    public string AlgorithmName => $"Selective-{_innerCompressor.AlgorithmName}";

    public SelectiveCompressionService(
        ICompressionService innerCompressor,
        int compressionThresholdBytes = 1024)
    {
        _innerCompressor = innerCompressor ?? throw new ArgumentNullException(nameof(innerCompressor));
        _compressionThresholdBytes = compressionThresholdBytes;
    }

    public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        // Don't compress small data
        if (data.Length < _compressionThresholdBytes)
        {
            return PrependMarker(data, UncompressedMarker);
        }

        // Compress larger data
        var compressed = await _innerCompressor.CompressAsync(data, cancellationToken);
        
        // Only use compression if it actually reduces size
        if (compressed.Length < data.Length)
        {
            return PrependMarker(compressed, CompressedMarker);
        }

        return PrependMarker(data, UncompressedMarker);
    }

    public async Task<byte[]> CompressAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<byte>();

        var data = System.Text.Encoding.UTF8.GetBytes(text);
        return await CompressAsync(data, cancellationToken);
    }

    public async Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        if (compressedData == null || compressedData.Length == 0)
            return Array.Empty<byte>();

        // Check marker
        var marker = compressedData[0];
        var dataWithoutMarker = compressedData.Skip(1).ToArray();

        if (marker == CompressedMarker)
        {
            return await _innerCompressor.DecompressAsync(dataWithoutMarker, cancellationToken);
        }

        return dataWithoutMarker;
    }

    public async Task<string> DecompressToStringAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        var decompressed = await DecompressAsync(compressedData, cancellationToken);
        return System.Text.Encoding.UTF8.GetString(decompressed);
    }

    private static byte[] PrependMarker(byte[] data, byte marker)
    {
        var result = new byte[data.Length + 1];
        result[0] = marker;
        Array.Copy(data, 0, result, 1, data.Length);
        return result;
    }
}
