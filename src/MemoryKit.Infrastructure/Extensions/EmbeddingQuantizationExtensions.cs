using MemoryKit.Infrastructure.Embeddings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryKit.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering embedding quantization services.
/// </summary>
public static class EmbeddingQuantizationExtensions
{
    /// <summary>
    /// Adds embedding quantization services to the DI container based on configuration.
    /// </summary>
    public static IServiceCollection AddEmbeddingQuantization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var enabled = configuration.GetValue("MemoryKit:Embeddings:QuantizationEnabled", false);
        
        if (!enabled)
        {
            // No quantization services registered
            return services;
        }

        var precision = configuration.GetValue("MemoryKit:Embeddings:Precision", "Int8");

        // Register quantizer based on precision setting
        switch (precision.ToUpperInvariant())
        {
            case "INT8":
                services.AddSingleton<IEmbeddingQuantizer, Int8EmbeddingQuantizer>();
                break;
            
            case "FLOAT32":
            case "NONE":
                // No quantization
                break;
            
            default:
                // Default to Int8
                services.AddSingleton<IEmbeddingQuantizer, Int8EmbeddingQuantizer>();
                break;
        }

        return services;
    }
}
