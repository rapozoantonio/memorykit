using MemoryKit.Infrastructure.Embeddings;
using Xunit;

namespace MemoryKit.Infrastructure.Tests.Embeddings;

public class Int8EmbeddingQuantizerTests
{
    private readonly Int8EmbeddingQuantizer _quantizer;

    public Int8EmbeddingQuantizerTests()
    {
        _quantizer = new Int8EmbeddingQuantizer();
    }

    [Fact]
    public void Quantizer_HasCorrectProperties()
    {
        // Assert
        Assert.Equal(EmbeddingPrecision.Int8, _quantizer.Precision);
        Assert.Equal(0.25, _quantizer.CompressionRatio); // 75% reduction
    }

    [Fact]
    public void Quantize_ValidEmbedding_ReturnsQuantizedData()
    {
        // Arrange
        var embedding = GenerateTestEmbedding(1536);

        // Act
        var quantized = _quantizer.Quantize(embedding);

        // Assert
        Assert.NotNull(quantized);
        Assert.Equal(1536, quantized.Data.Length);
        Assert.Equal(1536, quantized.Dimensions);
        Assert.Equal(EmbeddingPrecision.Int8, quantized.Precision);
        Assert.True(quantized.Scale > 0);
    }

    [Fact]
    public void Quantize_Then_Dequantize_RoundTrips()
    {
        // Arrange
        var original = GenerateTestEmbedding(1536);

        // Act
        var quantized = _quantizer.Quantize(original);
        var reconstructed = _quantizer.Dequantize(quantized);

        // Assert
        Assert.Equal(original.Length, reconstructed.Length);
        
        // Check that values are close (not exact due to quantization)
        for (int i = 0; i < original.Length; i++)
        {
            var error = Math.Abs(original[i] - reconstructed[i]);
            Assert.True(error < 0.01, $"Element {i}: error {error} too large");
        }
    }

    [Fact]
    public void Quantize_AchievesExpectedCompressionRatio()
    {
        // Arrange
        var embedding = GenerateTestEmbedding(1536);
        var originalSize = embedding.Length * sizeof(float); // 6144 bytes

        // Act
        var quantized = _quantizer.Quantize(embedding);
        var compressedSize = quantized.StorageSizeBytes;

        // Assert
        // Int8: 1536 bytes (data) + 4 (scale) + 4 (offset) + 8 (metadata) = 1552 bytes
        Assert.True(compressedSize < 1600, $"Compressed size {compressedSize} too large");
        
        var actualRatio = (double)compressedSize / originalSize;
        Assert.True(actualRatio < 0.26, $"Compression ratio {actualRatio:F2} not good enough");
    }

    [Fact]
    public void CalculateMAE_AfterQuantization_IsLow()
    {
        // Arrange
        var original = GenerateTestEmbedding(1536);

        // Act
        var quantized = _quantizer.Quantize(original);
        var reconstructed = _quantizer.Dequantize(quantized);
        var mae = Int8EmbeddingQuantizer.CalculateMAE(original, reconstructed);

        // Assert
        // MAE should be < 0.01 for good quantization
        Assert.True(mae < 0.01, $"MAE {mae:F6} too high");
    }

    [Fact]
    public void CalculateMSE_AfterQuantization_IsLow()
    {
        // Arrange
        var original = GenerateTestEmbedding(1536);

        // Act
        var quantized = _quantizer.Quantize(original);
        var reconstructed = _quantizer.Dequantize(quantized);
        var mse = Int8EmbeddingQuantizer.CalculateMSE(original, reconstructed);

        // Assert
        // MSE should be very small
        Assert.True(mse < 0.0001, $"MSE {mse:F8} too high");
    }

    [Fact]
    public void CosineSimilarity_PreservedAfterQuantization()
    {
        // Arrange - use somewhat similar embeddings (mix of same + different)
        var embedding1 = GenerateTestEmbedding(1536, seed: 42);
        var embedding2 = new float[1536];
        for (int i = 0; i < 1536; i++)
        {
            // Mix 70% of embedding1 with 30% random for moderate similarity
            embedding2[i] = embedding1[i] * 0.7f + GenerateTestEmbedding(1536, seed: 123)[i] * 0.3f;
        }

        // Calculate original similarity
        var originalSimilarity = CosineSimilarity(embedding1, embedding2);

        // Act
        var quantized1 = _quantizer.Quantize(embedding1);
        var quantized2 = _quantizer.Quantize(embedding2);
        var quantizedSimilarity = Int8EmbeddingQuantizer.CosineSimilarityQuantized(quantized1, quantized2);

        // Assert
        // Quantized similarity should be close to original (<= 10% relative error is acceptable for Int8)
        var error = Math.Abs(originalSimilarity - quantizedSimilarity);
        var relativeError = error / Math.Max(originalSimilarity, 0.01); // Avoid division by near-zero
        Assert.True(relativeError < 0.10, 
            $"Relative similarity error {relativeError:F4} too high (original: {originalSimilarity:F4}, quantized: {quantizedSimilarity:F4}, absolute error: {error:F4})");
    }

    [Fact]
    public void Quantize_NullEmbedding_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _quantizer.Quantize(null!));
    }

    [Fact]
    public void Quantize_EmptyEmbedding_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _quantizer.Quantize(Array.Empty<float>()));
    }

    [Fact]
    public void Dequantize_NullQuantized_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _quantizer.Dequantize(null!));
    }

    [Fact]
    public void Dequantize_WrongPrecision_ThrowsException()
    {
        // Arrange
        var quantized = new QuantizedEmbedding
        {
            Data = new byte[1536],
            Scale = 1.0f,
            Offset = 0.0f,
            Dimensions = 1536,
            Precision = EmbeddingPrecision.Float16 // Wrong precision
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _quantizer.Dequantize(quantized));
    }

    [Fact]
    public void Quantize_UniformValues_HandlesGracefully()
    {
        // Arrange - all values the same (edge case)
        var embedding = Enumerable.Repeat(0.5f, 1536).ToArray();

        // Act
        var quantized = _quantizer.Quantize(embedding);
        var reconstructed = _quantizer.Dequantize(quantized);

        // Assert
        Assert.All(reconstructed, value => Assert.InRange(value, 0.49f, 0.51f));
    }

    [Fact]
    public void Quantize_ExtremeValues_HandlesCorrectly()
    {
        // Arrange
        var embedding = new float[1536];
        for (int i = 0; i < 768; i++)
            embedding[i] = -1.0f;
        for (int i = 768; i < 1536; i++)
            embedding[i] = 1.0f;

        // Act
        var quantized = _quantizer.Quantize(embedding);
        var reconstructed = _quantizer.Dequantize(quantized);

        // Assert
        for (int i = 0; i < 768; i++)
            Assert.InRange(reconstructed[i], -1.01f, -0.99f);
        for (int i = 768; i < 1536; i++)
            Assert.InRange(reconstructed[i], 0.99f, 1.01f);
    }

    [Theory]
    [InlineData(128)]
    [InlineData(384)]
    [InlineData(768)]
    [InlineData(1536)]
    [InlineData(3072)]
    public void Quantize_DifferentDimensions_Works(int dimensions)
    {
        // Arrange
        var embedding = GenerateTestEmbedding(dimensions);

        // Act
        var quantized = _quantizer.Quantize(embedding);
        var reconstructed = _quantizer.Dequantize(quantized);

        // Assert
        Assert.Equal(dimensions, quantized.Dimensions);
        Assert.Equal(dimensions, reconstructed.Length);
    }

    [Fact]
    public void StorageSizeBytes_CalculatedCorrectly()
    {
        // Arrange
        var embedding = GenerateTestEmbedding(1536);

        // Act
        var quantized = _quantizer.Quantize(embedding);

        // Assert
        // Data: 1536 bytes + Scale: 4 bytes + Offset: 4 bytes + Dimensions: 4 + Precision: 4 = 1552 bytes
        var expectedSize = 1536 + sizeof(float) * 2 + sizeof(int) * 2;
        Assert.Equal(expectedSize, quantized.StorageSizeBytes);
    }

    // Helper methods

    private static float[] GenerateTestEmbedding(int dimensions, int seed = 42)
    {
        var random = new Random(seed);
        var embedding = new float[dimensions];
        
        for (int i = 0; i < dimensions; i++)
        {
            // Generate values in range [-1, 1] with normal distribution
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        // Normalize to unit length (typical for embeddings)
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

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same length");

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < a.Length; i++)
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
