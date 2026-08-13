using Documentation.Ingestion.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace Documentation.Ingestion.Infrastructure.Persistence;

public sealed class IngestionDbContext : DbContext
{
    public IngestionDbContext(DbContextOptions<IngestionDbContext> options)
        : base(options)
    {
    }

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        ConfigureDocumentChunks(modelBuilder.Entity<DocumentChunk>());
        ConfigureProcessedIntegrationEvents(modelBuilder.Entity<ProcessedIntegrationEvent>());
    }

    private static void ConfigureDocumentChunks(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks", "ingestion");
        builder.HasKey(chunk => chunk.Id);
        builder.Property(chunk => chunk.Id).HasColumnName("id");
        builder.Property(chunk => chunk.DocumentId).HasColumnName("document_id").IsRequired();
        builder.Property(chunk => chunk.VersionId).HasColumnName("version_id").IsRequired();
        builder.Property(chunk => chunk.ChunkIndex).HasColumnName("chunk_index").IsRequired();
        builder.Property(chunk => chunk.ChunkType).HasColumnName("chunk_type").HasMaxLength(80).IsRequired();
        builder.Property(chunk => chunk.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(chunk => chunk.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsRequired();
        builder.Property(chunk => chunk.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb").IsRequired();
        var embeddingProperty = builder.Property(chunk => chunk.Embedding)
            .HasColumnName("embedding")
            .HasConversion(
                embedding => new Vector(embedding),
                vector => vector.ToArray())
            .HasColumnType("vector(768)")
            .IsRequired();
        embeddingProperty.Metadata.SetValueComparer(new ValueComparer<float[]>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value)),
            values => values.ToArray()));
        builder.Property(chunk => chunk.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasIndex(chunk => new { chunk.VersionId, chunk.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("ux_document_chunks_version_chunk_index");
    }

    private static void ConfigureProcessedIntegrationEvents(EntityTypeBuilder<ProcessedIntegrationEvent> builder)
    {
        builder.ToTable("processed_integration_events", "ingestion");
        builder.HasKey(integrationEvent => integrationEvent.EventId);
        builder.Property(integrationEvent => integrationEvent.EventId).HasColumnName("event_id");
        builder.Property(integrationEvent => integrationEvent.ProcessedAtUtc).HasColumnName("processed_at_utc").IsRequired();
    }
}
