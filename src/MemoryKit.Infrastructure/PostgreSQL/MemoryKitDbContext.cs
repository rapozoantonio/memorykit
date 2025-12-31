using MemoryKit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace MemoryKit.Infrastructure.PostgreSQL;

/// <summary>
/// Entity Framework Core DbContext for MemoryKit PostgreSQL storage.
/// Manages all memory layer entities with proper indexing and constraints.
/// </summary>
public class MemoryKitDbContext : DbContext
{
    public MemoryKitDbContext(DbContextOptions<MemoryKitDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Working Memory table: Short-term, active context (<5ms access)
    /// </summary>
    public DbSet<WorkingMemoryEntity> WorkingMemories => Set<WorkingMemoryEntity>();

    /// <summary>
    /// Semantic Memory table: Facts and knowledge (<50ms access, with vector embeddings)
    /// </summary>
    public DbSet<SemanticFactEntity> SemanticFacts => Set<SemanticFactEntity>();

    /// <summary>
    /// Episodic Memory table: Events and temporal information (<100ms access)
    /// </summary>
    public DbSet<EpisodicEventEntity> EpisodicEvents => Set<EpisodicEventEntity>();

    /// <summary>
    /// Procedural Memory table: Learned patterns and behaviors (<200ms access)
    /// </summary>
    public DbSet<ProceduralPatternEntity> ProceduralPatterns => Set<ProceduralPatternEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Configure Working Memory
        modelBuilder.Entity<WorkingMemoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Importance).HasDefaultValue(0.5);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // Indexes for performance
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt })
                .IsDescending(false, true)
                .HasDatabaseName("idx_working_conv_created");

            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("idx_working_expires")
                .HasFilter("\"ExpiresAt\" IS NOT NULL");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_working_user");
        });

        // Configure Semantic Facts
        modelBuilder.Entity<SemanticFactEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Confidence).HasDefaultValue(0.95);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            // Configure Vector column type explicitly
            entity.Property(e => e.Embedding).HasColumnType("vector(1536)");

            // Vector search index (pgvector)
            entity.HasIndex("Embedding")
                .HasDatabaseName("idx_semantic_embedding")
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");

            // Standard indexes for performance
            entity.HasIndex(e => e.ConversationId)
                .HasDatabaseName("idx_semantic_conv");

            entity.HasIndex(e => e.FactType)
                .HasDatabaseName("idx_semantic_facttype");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_semantic_user");

            entity.HasIndex(e => e.Confidence)
                .HasDatabaseName("idx_semantic_confidence");
        });

        // Configure Episodic Events
        modelBuilder.Entity<EpisodicEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.OccurredAt).IsRequired();
            entity.Property(e => e.DecayFactor).HasDefaultValue(1.0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // Temporal indexes for efficiency
            entity.HasIndex(e => new { e.ConversationId, e.OccurredAt })
                .IsDescending(false, true)
                .HasDatabaseName("idx_episodic_conv_occurred");

            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("idx_episodic_eventtype");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_episodic_user");

            entity.HasIndex(e => e.OccurredAt)
                .HasDatabaseName("idx_episodic_occurred");
        });

        // Configure Procedural Patterns
        modelBuilder.Entity<ProceduralPatternEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.PatternName).IsRequired();
            entity.Property(e => e.TriggerConditions).IsRequired();
            entity.Property(e => e.LearnedResponse).IsRequired();
            entity.Property(e => e.SuccessCount).HasDefaultValue(0);
            entity.Property(e => e.FailureCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            // Indexes for efficient pattern retrieval
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_procedural_user");

            entity.HasIndex(e => e.PatternName)
                .HasDatabaseName("idx_procedural_pattern_name");

            entity.HasIndex(e => new { e.UserId, e.PatternName })
                .HasDatabaseName("idx_procedural_user_pattern");

            // Note: GIN index on text columns requires pg_trgm extension
            // Using standard BTREE index instead for now
            entity.HasIndex(e => e.TriggerConditions)
                .HasDatabaseName("idx_procedural_conditions");
        });
    }
}

/// <summary>
/// Entity model for Working Memory.
/// </summary>
public class WorkingMemoryEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Importance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? PromotedTo { get; set; } // Foreign key to SemanticFact
}

/// <summary>
/// Entity model for Semantic Memory.
/// </summary>
public class SemanticFactEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FactType { get; set; }
    public double Confidence { get; set; }
    public Vector? Embedding { get; set; } // pgvector type
    public string? Metadata { get; set; } // JSON data stored as TEXT
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Entity model for Episodic Memory.
/// </summary>
public class EpisodicEventEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Participants { get; set; } // JSON stored as TEXT
    public DateTime OccurredAt { get; set; }
    public double DecayFactor { get; set; }
    public string? Metadata { get; set; } // JSON stored as TEXT
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Entity model for Procedural Memory.
/// </summary>
public class ProceduralPatternEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public string TriggerConditions { get; set; } = string.Empty; // JSON stored as TEXT
    public string LearnedResponse { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? Metadata { get; set; } // JSON stored as TEXT
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
