using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Documentation.Ingestion.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private const string CreateVectorExtensionSql = "CREATE EXTENSION IF NOT EXISTS vector;";
    private const string CreateSchemaSql = "CREATE SCHEMA IF NOT EXISTS ingestion;";
    private const string CreateDocumentChunksTableSql = """
        CREATE TABLE IF NOT EXISTS ingestion.document_chunks (
            id uuid PRIMARY KEY,
            document_id uuid NOT NULL,
            version_id uuid NOT NULL,
            chunk_index integer NOT NULL,
            chunk_type character varying(80) NOT NULL,
            content text NOT NULL,
            content_hash character varying(64) NOT NULL,
            metadata jsonb NOT NULL,
            embedding vector(1024) NOT NULL,
            created_at_utc timestamp with time zone NOT NULL,
            CONSTRAINT ux_document_chunks_version_chunk_index UNIQUE (version_id, chunk_index)
        );
        """;
    private const string CreateProcessedEventsTableSql = """
        CREATE TABLE IF NOT EXISTS ingestion.processed_integration_events (
            event_id uuid PRIMARY KEY,
            processed_at_utc timestamp with time zone NOT NULL
        );
        """;
    private const string CreateHnswIndexSql =
        "CREATE INDEX IF NOT EXISTS ix_document_chunks_embedding_hnsw " +
        "ON ingestion.document_chunks USING hnsw (embedding vector_cosine_ops);";

    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseInitializer(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IngestionDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(CreateVectorExtensionSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(CreateSchemaSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(CreateDocumentChunksTableSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(CreateProcessedEventsTableSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(CreateHnswIndexSql, cancellationToken);

        _logger.LogInformation("Ingestion database is ready.");
    }
}
