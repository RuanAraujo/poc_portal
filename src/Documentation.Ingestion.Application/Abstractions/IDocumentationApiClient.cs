using Documentation.Ingestion.Application.Models;

namespace Documentation.Ingestion.Application.Abstractions;

public interface IDocumentationApiClient
{
    Task<DocumentationContent> GetContentAsync(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken);

    Task UpdateIndexingStatusAsync(
        Guid documentId,
        Guid versionId,
        DocumentationIndexingStatus status,
        CancellationToken cancellationToken);
}
