using MemoryKit.Infrastructure.PostgreSQL;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using MemoryKit.Infrastructure.PostgreSQL.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pgvector.EntityFrameworkCore;

namespace MemoryKit.Infrastructure.Extensions;

/// <summary>
/// Dependency injection extensions for PostgreSQL storage provider.
/// </summary>
public static class PostgresServiceCollectionExtensions
{
    /// <summary>
    /// Adds PostgreSQL storage provider to the service collection.
    /// </summary>
    public static IServiceCollection AddPostgresStorage(
        this IServiceCollection services,
        string connectionString)
    {
        // Add DbContext Factory (for SINGLETON services to create DbContext per operation)
        services.AddDbContextFactory<MemoryKitDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(2),
                    errorCodesToAdd: null);
                npgsqlOptions.UseVector();
            });
        });

        // Register repositories as SINGLETON (they use IDbContextFactory for per-operation DbContext)
        services.AddSingleton<IWorkingMemoryRepository, PostgresWorkingMemoryRepository>();
        services.AddSingleton<ISemanticMemoryRepository, PostgresSemanticMemoryRepository>();
        services.AddSingleton<IEpisodicMemoryRepository, PostgresEpisodicMemoryRepository>();
        services.AddSingleton<IProceduralMemoryRepository, PostgresProceduralMemoryRepository>();

        // Register services as SINGLETON (they use IDbContextFactory for per-operation DbContext)
        services.AddSingleton<PostgresWorkingMemoryService>();
        services.AddSingleton<PostgresScratchpadService>();
        services.AddSingleton<PostgresEpisodicMemoryService>();
        services.AddSingleton<PostgresProceduralMemoryService>();

        // Auto-migrate on startup
        services.AddHostedService<DbMigrationService>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations on application startup.
    /// </summary>
    private class DbMigrationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DbMigrationService> _logger;

        public DbMigrationService(IServiceProvider serviceProvider, ILogger<DbMigrationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<MemoryKitDbContext>>();
            await using (var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken))
            {
                try
                {
                    _logger.LogInformation("Ensuring database exists and creating schema...");
                    await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                    _logger.LogInformation("Database schema created successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating database schema");
                    throw;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
