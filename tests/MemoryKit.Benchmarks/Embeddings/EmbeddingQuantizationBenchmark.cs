using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MemoryKit.Infrastructure.Embeddings;

namespace MemoryKit.Benchmarks.Embeddings;

/// <summary>
/// Benchmark comparing embedding quantization performance and accuracy.
/// Run with: dotnet run --project tests/MemoryKit.Benchmarks -c Release --filter *Embedding*
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class EmbeddingQuantizationBenchmark
{
    private float[] _embedding1536 = null!;
    private float[] _embedding768 = null!;
    private Int8EmbeddingQuantizer _quantizer = null!;
    private QuantizedEmbedding _quantized1536 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _quantizer = new Int8EmbeddingQuantizer();
        _embedding1536 = GenerateEmbedding(1536);
        _embedding768 = GenerateEmbedding(768);
        _quantized1536 = _quantizer.Quantize(_embedding1536);
    }

    // ========== Quantization Performance ==========

    [Benchmark(Description = "Quantize 1536-dim (OpenAI ada-002)")]
    public QuantizedEmbedding Quantize_1536Dimensions()
    {
        return _quantizer.Quantize(_embedding1536);
    }

    [Benchmark(Description = "Quantize 768-dim (BERT)")]
    public QuantizedEmbedding Quantize_768Dimensions()
    {
        return _quantizer.Quantize(_embedding768);
    }

    [Benchmark(Description = "Dequantize 1536-dim")]
    public float[] Dequantize_1536Dimensions()
    {
        return _quantizer.Dequantize(_quantized1536);
    }

    [Benchmark(Description = "Round-trip (quantize + dequantize)")]
    public float[] RoundTrip_1536Dimensions()
    {
        var quantized = _quantizer.Quantize(_embedding1536);
        return _quantizer.Dequantize(quantized);
    }

    // ========== Storage Size Comparison ==========

    [Benchmark(Baseline = true, Description = "Storage: Float32 (baseline)")]
    public int StorageSize_Float32()
    {
        return _embedding1536.Length * sizeof(float); // 6144 bytes
    }

    [Benchmark(Description = "Storage: Int8 quantized")]
    public int StorageSize_Int8()
    {
        return _quantized1536.StorageSizeBytes; // ~1552 bytes
    }

    // ========== Similarity Search Performance ==========

    [Benchmark(Description = "Cosine similarity: Float32")]
    public double CosineSimilarity_Float32()
    {
        return CalculateCosineSimilarity(_embedding1536, _embedding768);
    }

    [Benchmark(Description = "Cosine similarity: Int8 quantized")]
    public double CosineSimilarity_Int8Quantized()
    {
        var quantized768 = _quantizer.Quantize(_embedding768);
        return Int8EmbeddingQuantizer.CosineSimilarityQuantized(_quantized1536, quantized768);
    }

    // Helper methods
    private static float[] GenerateEmbedding(int dimensions)
    {
        var random = new Random(42);
        var embedding = new float[dimensions];
        
        for (int i = 0; i < dimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        Normalize(embedding);
        return embedding;
    }

    private static void Normalize(float[] vector)
    {
        double sumSquares = 0.0;
        for (int i = 0; i < vector.Length; i++)
            sumSquares += vector[i] * vector[i];
        
        var magnitude = Math.Sqrt(sumSquares);
        if (magnitude > 1e-10)
        {
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= (float)magnitude;
        }
    }

    private static double CalculateCosineSimilarity(float[] a, float[] b)
    {
        // Pad or truncate to match lengths
        int minLen = Math.Min(a.Length, b.Length);
        
        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < minLen; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

/// <summary>
/// Benchmark measuring accuracy loss from quantization.
/// </summary>
[MemoryDiagnoser]
public class EmbeddingAccuracyBenchmark
{
    [Params(128, 384, 768, 1536, 3072)]
    public int Dimensions { get; set; }

    private float[] _embedding = null!;
    private Int8EmbeddingQuantizer _quantizer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _quantizer = new Int8EmbeddingQuantizer();
        _embedding = GenerateEmbedding(Dimensions);
    }

    [Benchmark(Description = "Mean Absolute Error (MAE)")]
    public double MeasureMAE()
    {
        var quantized = _quantizer.Quantize(_embedding);
        var reconstructed = _quantizer.Dequantize(quantized);
        return Int8EmbeddingQuantizer.CalculateMAE(_embedding, reconstructed);
    }

    [Benchmark(Description = "Mean Squared Error (MSE)")]
    public double MeasureMSE()
    {
        var quantized = _quantizer.Quantize(_embedding);
        var reconstructed = _quantizer.Dequantize(quantized);
        return Int8EmbeddingQuantizer.CalculateMSE(_embedding, reconstructed);
    }

    [Benchmark(Description = "Compression ratio")]
    public double CompressionRatio()
    {
        var quantized = _quantizer.Quantize(_embedding);
        var originalSize = _embedding.Length * sizeof(float);
        return (double)quantized.StorageSizeBytes / originalSize;
    }

    private static float[] GenerateEmbedding(int dimensions)
    {
        var random = new Random(42);
        var embedding = new float[dimensions];
        
        for (int i = 0; i < dimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        Normalize(embedding);
        return embedding;
    }

    private static void Normalize(float[] vector)
    {
        double sumSquares = 0.0;
        for (int i = 0; i < vector.Length; i++)
            sumSquares += vector[i] * vector[i];
        
        var magnitude = Math.Sqrt(sumSquares);
        if (magnitude > 1e-10)
        {
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= (float)magnitude;
        }
    }
}
