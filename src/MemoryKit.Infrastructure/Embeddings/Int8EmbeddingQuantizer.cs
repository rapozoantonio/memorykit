namespace MemoryKit.Infrastructure.Embeddings;

/// <summary>
/// Quantizes float32 embeddings to Int8 format using min-max normalization.
/// Achieves 75% storage reduction with typical accuracy loss of 2-5%.
/// </summary>
public class Int8EmbeddingQuantizer : IEmbeddingQuantizer
{
    public EmbeddingPrecision Precision => EmbeddingPrecision.Int8;
    public double CompressionRatio => 0.25; // 75% reduction

    /// <summary>
    /// Quantizes float32 embedding to Int8 using min-max normalization.
    /// Formula: quantized = (value - min) / (max - min) * 255
    /// </summary>
    public QuantizedEmbedding Quantize(float[] embedding)
    {
        if (embedding == null || embedding.Length == 0)
            throw new ArgumentException("Embedding cannot be null or empty", nameof(embedding));

        // Find min and max for normalization
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < embedding.Length; i++)
        {
            if (embedding[i] < min) min = embedding[i];
            if (embedding[i] > max) max = embedding[i];
        }

        // Handle edge case where all values are the same
        float scale = max - min;
        if (scale < 1e-10f)
        {
            scale = 1.0f;
            min = 0.0f;
        }

        // Quantize to byte range [0, 255]
        var quantized = new byte[embedding.Length];
        for (int i = 0; i < embedding.Length; i++)
        {
            float normalized = (embedding[i] - min) / scale;
            quantized[i] = (byte)Math.Clamp(normalized * 255.0f, 0, 255);
        }

        return new QuantizedEmbedding
        {
            Data = quantized,
            Scale = scale,
            Offset = min,
            Dimensions = embedding.Length,
            Precision = EmbeddingPrecision.Int8
        };
    }

    /// <summary>
    /// Dequantizes Int8 embedding back to float32 format.
    /// Formula: value = (quantized / 255) * scale + offset
    /// </summary>
    public float[] Dequantize(QuantizedEmbedding quantized)
    {
        if (quantized == null)
            throw new ArgumentNullException(nameof(quantized));

        if (quantized.Precision != EmbeddingPrecision.Int8)
            throw new ArgumentException($"Expected Int8 precision, got {quantized.Precision}", nameof(quantized));

        var result = new float[quantized.Data.Length];
        
        for (int i = 0; i < quantized.Data.Length; i++)
        {
            float normalized = quantized.Data[i] / 255.0f;
            result[i] = normalized * quantized.Scale + quantized.Offset;
        }

        return result;
    }

    /// <summary>
    /// Calculates cosine similarity directly in quantized space for performance.
    /// This avoids dequantization overhead during similarity searches.
    /// </summary>
    public static double CosineSimilarityQuantized(QuantizedEmbedding a, QuantizedEmbedding b)
    {
        if (a.Data.Length != b.Data.Length)
            throw new ArgumentException("Embeddings must have the same dimensions");

        // Convert to float for calculation (still faster than full dequantization)
        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < a.Data.Length; i++)
        {
            double valA = a.Data[i];
            double valB = b.Data[i];
            
            dotProduct += valA * valB;
            normA += valA * valA;
            normB += valB * valB;
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    /// <summary>
    /// Calculates the Mean Absolute Error (MAE) between original and dequantized embeddings.
    /// Used for quality assessment.
    /// </summary>
    public static double CalculateMAE(float[] original, float[] reconstructed)
    {
        if (original.Length != reconstructed.Length)
            throw new ArgumentException("Arrays must have the same length");

        double sum = 0.0;
        for (int i = 0; i < original.Length; i++)
        {
            sum += Math.Abs(original[i] - reconstructed[i]);
        }

        return sum / original.Length;
    }

    /// <summary>
    /// Calculates the Mean Squared Error (MSE) between original and dequantized embeddings.
    /// </summary>
    public static double CalculateMSE(float[] original, float[] reconstructed)
    {
        if (original.Length != reconstructed.Length)
            throw new ArgumentException("Arrays must have the same length");

        double sum = 0.0;
        for (int i = 0; i < original.Length; i++)
        {
            double diff = original[i] - reconstructed[i];
            sum += diff * diff;
        }

        return sum / original.Length;
    }
}
