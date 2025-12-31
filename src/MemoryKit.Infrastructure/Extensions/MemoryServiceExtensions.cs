using MemoryKit.Domain.Interfaces;
using MemoryKit.Infrastructure.Azure;
using MemoryKit.Infrastructure.Factories;
using MemoryKit.Infrastructure.InMemory;
using MemoryKit.Infrastructure.PostgreSQL;
using MemoryKit.Infrastructure.SQLite;
using MemoryKit.Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Search.Documents;
using Azure;

namespace MemoryKit.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring memory services in DI container.
/// </summary>
public static class MemoryServiceExtensions
{
    /// <summary>
    /// Adds all memory services to the DI container based on configuration.
    /// </summary>
    public static IServiceCollection AddMemoryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get storage provider from configuration
        var providerString = configuration.GetValue<string>("MemoryKit:StorageProvider") ?? "InMemory";
        var isAzure = providerString.Equals("Azure", StringComparison.OrdinalIgnoreCase);
        var isPostgres = providerString.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);
        var isSqlite = providerString.Equals("SQLite", StringComparison.OrdinalIgnoreCase);

        // Register InMemory services (always available as fallback)
        services.AddSingleton<InMemoryWorkingMemoryService>();
        services.AddSingleton<InMemoryScratchpadService>();
        services.AddSingleton<EnhancedInMemoryProceduralMemoryService>();
        services.AddSingleton<InMemoryEpisodicMemoryService>();

        // Register PostgreSQL storage if configured
        if (isPostgres)
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL") 
                ?? throw new InvalidOperationException("PostgreSQL connection string not configured");
            services.AddPostgresStorage(connectionString);
        }

        // Register SQLite storage if configured
        if (isSqlite)
        {
            services.AddSqliteStorage();
        }

        // Register Azure services if configured
        if (isAzure)
        {
            RegisterAzureClients(services, configuration);
            RegisterAzureServices(services);
        }

        // Register factory
        services.AddSingleton<MemoryServiceFactory>();

        // Register primary interfaces using factory
        services.AddSingleton<IWorkingMemoryService>(sp =>
        {
            var factory = sp.GetRequiredService<MemoryServiceFactory>();
            var primary = factory.CreateWorkingMemoryService();
            
            var enableFallback = configuration.GetValue("MemoryKit:EnableAutoFallback", true);
            if (enableFallback && isAzure)
            {
                var fallback = sp.GetRequiredService<InMemoryWorkingMemoryService>();
                return new ResilientWorkingMemoryService(primary, fallback, configuration, 
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResilientWorkingMemoryService>>());
            }
            
            return primary;
        });

        services.AddSingleton<IScratchpadService>(sp =>
        {
            var factory = sp.GetRequiredService<MemoryServiceFactory>();
            return factory.CreateScratchpadService();
        });

        services.AddSingleton<IProceduralMemoryService>(sp =>
        {
            var factory = sp.GetRequiredService<MemoryServiceFactory>();
            return factory.CreateProceduralMemoryService();
        });

        services.AddSingleton<IEpisodicMemoryService>(sp =>
        {
            var factory = sp.GetRequiredService<MemoryServiceFactory>();
            return factory.CreateEpisodicMemoryService();
        });

        return services;
    }

    private static void RegisterAzureClients(IServiceCollection services, IConfiguration configuration)
    {
        // Register Redis connection
        var redisConnection = configuration.GetValue<string>("MemoryKit:Azure:RedisConnectionString");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnection));
        }

        // Register Azure Storage connection
        var storageConnection = configuration.GetValue<string>("MemoryKit:Azure:StorageConnectionString");
        if (!string.IsNullOrEmpty(storageConnection))
        {
            // Table Storage clients
            services.AddSingleton(sp => new TableServiceClient(storageConnection));

            // Blob Storage client
            services.AddSingleton(sp => new BlobServiceClient(storageConnection));
        }

        // Register Azure AI Search
        var searchEndpoint = configuration.GetValue<string>("MemoryKit:Azure:SearchEndpoint");
        var searchApiKey = configuration.GetValue<string>("MemoryKit:Azure:SearchApiKey");
        if (!string.IsNullOrEmpty(searchEndpoint) && !string.IsNullOrEmpty(searchApiKey))
        {
            services.AddSingleton(sp =>
            {
                var indexName = configuration.GetValue<string>("MemoryKit:Azure:SearchIndexName") ?? "memorykit-episodic";
                return new SearchClient(new Uri(searchEndpoint), indexName, new AzureKeyCredential(searchApiKey));
            });
        }
    }

    private static void RegisterAzureServices(IServiceCollection services)
    {
        services.AddSingleton<AzureRedisWorkingMemoryService>();
        services.AddSingleton<AzureTableScratchpadService>();
        services.AddSingleton<AzureTableProceduralMemoryService>();
        services.AddSingleton<AzureBlobEpisodicMemoryService>();
    }
}
