using MemoryKit.Infrastructure.SQLite;
using MemoryKit.Infrastructure.SQLite.Repositories;
using MemoryKit.Infrastructure.PostgreSQL;
using MemoryKit.Infrastructure.PostgreSQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Extensions;

/// <summary>
/// Dependency injection extensions for SQLite storage provider.
/// </summary>
public static class SqliteServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLite storage provider to the service collection.
    /// Database file is created at ~/.memorykit/memories.db (with fallback to ./memories.db)
    /// </summary>
    public static IServiceCollection AddSqliteStorage(
        this IServiceCollection services,
        string? databasePath = null)
    {
        var dbPath = databasePath ?? GetDefaultDatabasePath();
        EnsureDatabaseDirectory(dbPath);

        // Add DbContext for SQLite
        services.AddDbContext<SqliteMemoryKitDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Also register as MemoryKitDbContext for compatibility
        services.AddScoped(sp =>
        {
            var sqliteContext = sp.GetRequiredService<SqliteMemoryKitDbContext>();
            return (MemoryKitDbContext)sqliteContext;
        });

        // Register SQLite repositories
        services.AddScoped<IWorkingMemoryRepository, SqliteWorkingMemoryRepository>();
        services.AddScoped<ISemanticMemoryRepository, SqliteSemanticMemoryRepository>();
        services.AddScoped<IEpisodicMemoryRepository, SqliteEpisodicMemoryRepository>();
        services.AddScoped<IProceduralMemoryRepository, SqliteProceduralMemoryRepository>();

        // Auto-migrate on startup
        services.AddHostedService<SqliteDbMigrationService>();

        return services;
    }

    /// <summary>
    /// Gets the default database path: ~/.memorykit/memories.db
    /// Falls back to ./memories.db if home directory is not writable.
    /// </summary>
    private static string GetDefaultDatabasePath()
    {
        try
        {
            var memoryKitDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".memorykit"
            );

            // Ensure directory exists
            Directory.CreateDirectory(memoryKitDir);

            return Path.Combine(memoryKitDir, "memories.db");
        }
        catch (System.IO.DirectoryNotFoundException)
        {
            // Fallback to current directory if home directory not accessible
            return Path.Combine(Directory.GetCurrentDirectory(), "memories.db");
        }
    }

    /// <summary>
    /// Ensures the database directory exists and is writable.
    /// </summary>
    private static void EnsureDatabaseDirectory(string databasePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch (System.IO.DirectoryNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not create database directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies pending migrations for SQLite on application startup.
    /// </summary>
    private class SqliteDbMigrationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SqliteDbMigrationService> _logger;

        public SqliteDbMigrationService(IServiceProvider serviceProvider, ILogger<SqliteDbMigrationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SqliteMemoryKitDbContext>();
                try
                {
                    _logger.LogInformation("Initializing SQLite database...");
                    
                    // Create database if it doesn't exist
                    await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                    _logger.LogInformation("SQLite database initialized successfully. Location: {Location}", 
                        GetDefaultDatabasePath());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing SQLite database");
                    throw;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
