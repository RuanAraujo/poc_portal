using Documentation.Ingestion.Domain.Entities;

namespace Documentation.Ingestion.Application.Abstractions;

public interface IChunkRepository
{
    Task ReplaceForVersionAsync(
        Guid documentId,
        Guid versionId,
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken);
}
