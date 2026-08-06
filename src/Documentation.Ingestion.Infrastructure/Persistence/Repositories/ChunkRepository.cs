using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Documentation.Ingestion.Infrastructure.Persistence.Repositories;

public sealed class ChunkRepository : IChunkRepository
{
    private readonly IngestionDbContext _dbContext;

    public ChunkRepository(IngestionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ReplaceForVersionAsync(
        Guid documentId,
        Guid versionId,
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        await _dbContext.DocumentChunks
            .Where(chunk => chunk.VersionId == versionId)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
    }
}
