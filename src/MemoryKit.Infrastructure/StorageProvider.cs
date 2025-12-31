namespace MemoryKit.Infrastructure;

/// <summary>
/// Defines the available storage providers for MemoryKit services.
/// </summary>
public enum StorageProvider
{
    /// <summary>
    /// In-memory storage (default for development/testing)
    /// </summary>
    InMemory,

    /// <summary>
    /// PostgreSQL with pgvector for vector search (recommended for Docker self-hosted)
    /// </summary>
    PostgreSQL,

    /// <summary>
    /// SQLite for local/CLI deployments (zero-config, single file)
    /// </summary>
    SQLite,

    /// <summary>
    /// Azure cloud services (Redis, Table Storage, Blob Storage, AI Search)
    /// </summary>
    Azure
}

/// <summary>
/// Configuration for MemoryKit storage provider selection and behavior.
/// </summary>
public class MemoryKitConfiguration
{
    /// <summary>
    /// The storage provider to use for memory services.
    /// </summary>
    public StorageProvider Provider { get; set; } = StorageProvider.InMemory;

    /// <summary>
    /// Enable automatic fallback to in-memory storage if Azure services fail.
    /// </summary>
    public bool EnableAutoFallback { get; set; } = true;

    /// <summary>
    /// Maximum retry attempts for Azure operations before falling back.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Health check interval for Azure services (in seconds).
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;
}
