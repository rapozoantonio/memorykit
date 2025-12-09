namespace MemoryKit.Infrastructure.Embeddings;

/// <summary>
/// Defines quantization operations for embedding vectors to reduce storage size.
/// </summary>
public interface IEmbeddingQuantizer
{
    /// <summary>
    /// Quantizes a float32 embedding vector to a compressed format.
    /// </summary>
    /// <param name="embedding">The float32 embedding vector (typically 1536 dimensions).</param>
    /// <returns>Quantized embedding data.</returns>
    QuantizedEmbedding Quantize(float[] embedding);

    /// <summary>
    /// Dequantizes compressed embedding data back to float32 format.
    /// </summary>
    /// <param name="quantized">The quantized embedding data.</param>
    /// <returns>Reconstructed float32 embedding vector.</returns>
    float[] Dequantize(QuantizedEmbedding quantized);

    /// <summary>
    /// Gets the quantization precision level.
    /// </summary>
    EmbeddingPrecision Precision { get; }

    /// <summary>
    /// Gets the expected compression ratio (0.0 to 1.0, where 0.25 = 75% reduction).
    /// </summary>
    double CompressionRatio { get; }
}

/// <summary>
/// Represents a quantized embedding with metadata for reconstruction.
/// </summary>
public record QuantizedEmbedding
{
    /// <summary>
    /// Quantized embedding data (Int8 format).
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Scale factor for denormalization (max - min).
    /// </summary>
    public required float Scale { get; init; }

    /// <summary>
    /// Offset for denormalization (min value).
    /// </summary>
    public required float Offset { get; init; }

    /// <summary>
    /// Number of dimensions in the embedding.
    /// </summary>
    public required int Dimensions { get; init; }

    /// <summary>
    /// Quantization precision used.
    /// </summary>
    public required EmbeddingPrecision Precision { get; init; }

    /// <summary>
    /// Calculates the total storage size in bytes.
    /// </summary>
    public int StorageSizeBytes => Data.Length + sizeof(float) * 2 + sizeof(int) * 2;
}

/// <summary>
/// Embedding precision levels.
/// </summary>
public enum EmbeddingPrecision
{
    /// <summary>
    /// Full precision (4 bytes per dimension) - no compression.
    /// </summary>
    Float32 = 0,

    /// <summary>
    /// Half precision (2 bytes per dimension) - 50% compression.
    /// </summary>
    Float16 = 1,

    /// <summary>
    /// 8-bit integer quantization (1 byte per dimension) - 75% compression.
    /// Recommended for most use cases (2-5% accuracy loss).
    /// </summary>
    Int8 = 2,

    /// <summary>
    /// 4-bit integer quantization (0.5 bytes per dimension) - 87.5% compression.
    /// Higher accuracy loss (~5-10%), experimental.
    /// </summary>
    Int4 = 3
}
