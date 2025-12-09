using MemoryKit.Infrastructure.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering compression services.
/// </summary>
public static class CompressionServiceExtensions
{
    /// <summary>
    /// Adds compression services to the DI container based on configuration.
    /// </summary>
    public static IServiceCollection AddCompressionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var compressionEnabled = configuration.GetValue("MemoryKit:Compression:Enabled", false);
        
        if (!compressionEnabled)
        {
            // No compression services registered
            return services;
        }

        var algorithm = configuration.GetValue("MemoryKit:Compression:Algorithm", "Selective-GZip");
        var thresholdBytes = configuration.GetValue("MemoryKit:Compression:ThresholdBytes", 1024);
        var asyncQueueEnabled = configuration.GetValue("MemoryKit:Compression:AsyncQueueEnabled", false);
        var asyncQueueWorkers = configuration.GetValue("MemoryKit:Compression:AsyncQueueWorkers", 2);
        var asyncQueueMaxSize = configuration.GetValue("MemoryKit:Compression:AsyncQueueMaxSize", 1000);

        // Register base compression services
        services.AddSingleton<GzipCompressionService>();
        services.AddSingleton<BrotliCompressionService>();

        // Register the configured compression service
        if (algorithm.StartsWith("Selective-", StringComparison.OrdinalIgnoreCase))
        {
            var innerAlgorithm = algorithm.Substring("Selective-".Length);
            
            services.AddSingleton<ICompressionService>(sp =>
            {
                ICompressionService innerService = innerAlgorithm.ToUpperInvariant() switch
                {
                    "GZIP" => sp.GetRequiredService<GzipCompressionService>(),
                    "BROTLI" => sp.GetRequiredService<BrotliCompressionService>(),
                    _ => sp.GetRequiredService<GzipCompressionService>()
                };

                return new SelectiveCompressionService(innerService, thresholdBytes);
            });
        }
        else if (algorithm.Equals("GZip", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICompressionService>(sp => 
                sp.GetRequiredService<GzipCompressionService>());
        }
        else if (algorithm.Equals("Brotli", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICompressionService>(sp => 
                sp.GetRequiredService<BrotliCompressionService>());
        }
        else
        {
            // Default to GZip
            services.AddSingleton<ICompressionService>(sp => 
                sp.GetRequiredService<GzipCompressionService>());
        }

        // Optionally register async compression queue
        if (asyncQueueEnabled)
        {
            services.AddSingleton<AsyncCompressionQueue>(sp =>
            {
                var compressionService = sp.GetRequiredService<ICompressionService>();
                var logger = sp.GetRequiredService<ILogger<AsyncCompressionQueue>>();
                
                return new AsyncCompressionQueue(
                    compressionService,
                    logger,
                    asyncQueueWorkers,
                    asyncQueueMaxSize);
            });
        }

        return services;
    }
}
