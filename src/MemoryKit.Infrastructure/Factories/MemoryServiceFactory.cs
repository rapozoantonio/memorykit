using MemoryKit.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Factories;

/// <summary>
/// Factory for creating memory service implementations based on configuration.
/// </summary>
public class MemoryServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MemoryServiceFactory> _logger;

    public MemoryServiceFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<MemoryServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Creates the appropriate working memory service based on configuration.
    /// </summary>
    public IWorkingMemoryService CreateWorkingMemoryService()
    {
        var provider = GetStorageProvider();
        
        _logger.LogInformation("Creating working memory service with provider: {Provider}", provider);

        return provider switch
        {
            StorageProvider.Azure => _serviceProvider.GetRequiredService<Azure.AzureRedisWorkingMemoryService>(),
            StorageProvider.InMemory => _serviceProvider.GetRequiredService<InMemory.InMemoryWorkingMemoryService>(),
            _ => throw new InvalidOperationException($"Unknown storage provider: {provider}")
        };
    }

    /// <summary>
    /// Creates the appropriate scratchpad service based on configuration.
    /// </summary>
    public IScratchpadService CreateScratchpadService()
    {
        var provider = GetStorageProvider();
        
        _logger.LogInformation("Creating scratchpad service with provider: {Provider}", provider);

        return provider switch
        {
            StorageProvider.Azure => _serviceProvider.GetRequiredService<Azure.AzureTableScratchpadService>(),
            StorageProvider.InMemory => _serviceProvider.GetRequiredService<InMemory.InMemoryScratchpadService>(),
            _ => throw new InvalidOperationException($"Unknown storage provider: {provider}")
        };
    }

    /// <summary>
    /// Creates the appropriate procedural memory service based on configuration.
    /// </summary>
    public IProceduralMemoryService CreateProceduralMemoryService()
    {
        var provider = GetStorageProvider();
        
        _logger.LogInformation("Creating procedural memory service with provider: {Provider}", provider);

        return provider switch
        {
            StorageProvider.Azure => _serviceProvider.GetRequiredService<Azure.AzureTableProceduralMemoryService>(),
            StorageProvider.InMemory => _serviceProvider.GetRequiredService<InMemory.EnhancedInMemoryProceduralMemoryService>(),
            _ => throw new InvalidOperationException($"Unknown storage provider: {provider}")
        };
    }

    /// <summary>
    /// Creates the appropriate episodic memory service based on configuration.
    /// </summary>
    public IEpisodicMemoryService CreateEpisodicMemoryService()
    {
        var provider = GetStorageProvider();
        
        _logger.LogInformation("Creating episodic memory service with provider: {Provider}", provider);

        return provider switch
        {
            StorageProvider.Azure => _serviceProvider.GetRequiredService<Azure.AzureBlobEpisodicMemoryService>(),
            StorageProvider.InMemory => _serviceProvider.GetRequiredService<InMemory.InMemoryEpisodicMemoryService>(),
            _ => throw new InvalidOperationException($"Unknown storage provider: {provider}")
        };
    }

    /// <summary>
    /// Gets the configured storage provider from configuration.
    /// </summary>
    private StorageProvider GetStorageProvider()
    {
        var providerString = _configuration.GetValue<string>("MemoryKit:StorageProvider") ?? "InMemory";
        
        if (!Enum.TryParse<StorageProvider>(providerString, ignoreCase: true, out var provider))
        {
            _logger.LogWarning(
                "Invalid storage provider '{Provider}' in configuration, defaulting to InMemory",
                providerString);
            return StorageProvider.InMemory;
        }

        return provider;
    }
}
