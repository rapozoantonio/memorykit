namespace MemoryKit.Infrastructure.Compression;

/// <summary>
/// Defines compression operations for memory storage optimization.
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Compresses data asynchronously.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compressed data.</returns>
    Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compresses a string asynchronously.
    /// </summary>
    /// <param name="text">The text to compress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compressed data.</returns>
    Task<byte[]> CompressAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses data asynchronously.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decompressed data.</returns>
    Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses data to string asynchronously.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decompressed string.</returns>
    Task<string> DecompressToStringAsync(byte[] compressedData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the compression algorithm name.
    /// </summary>
    string AlgorithmName { get; }
}
