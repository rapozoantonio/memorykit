using MemoryKit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Pgvector;

namespace MemoryKit.Infrastructure.SQLite;

/// <summary>
/// Entity Framework Core DbContext for MemoryKit SQLite storage.
/// Reuses the PostgreSQL DbContext schema but configured for SQLite.
/// Database file is created at ~/.memorykit/memories.db (with fallback to ./memories.db)
/// </summary>
public class SqliteMemoryKitDbContext : MemoryKit.Infrastructure.PostgreSQL.MemoryKitDbContext
{
    private readonly string _databasePath;

    public SqliteMemoryKitDbContext(string? databasePath = null)
        : base(GetDbContextOptions(databasePath))
    {
        _databasePath = databasePath ?? GetDefaultDatabasePath();
        EnsureDatabaseDirectory(_databasePath);
    }

    private static DbContextOptions<MemoryKit.Infrastructure.PostgreSQL.MemoryKitDbContext> GetDbContextOptions(string? databasePath)
    {
        var dbPath = databasePath ?? GetDefaultDatabasePath();
        EnsureDatabaseDirectory(dbPath);
        
        var builder = new DbContextOptionsBuilder<MemoryKit.Infrastructure.PostgreSQL.MemoryKitDbContext>();
        builder.UseSqlite($"Data Source={dbPath}");
        return builder.Options;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            EnsureDatabaseDirectory(_databasePath);
            optionsBuilder.UseSqlite($"Data Source={_databasePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Vector property to use JSON serialization for SQLite
        var vectorConverter = new ValueConverter<Vector?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v.ToArray(), (JsonSerializerOptions?)null),
            v => v == null ? null : new Vector(JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<float>())
        );

        modelBuilder.Entity<MemoryKit.Infrastructure.PostgreSQL.SemanticFactEntity>()
            .Property(e => e.Embedding)
            .HasConversion(vectorConverter);
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
}

