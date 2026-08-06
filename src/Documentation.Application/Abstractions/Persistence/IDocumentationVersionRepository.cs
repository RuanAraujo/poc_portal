using Documentation.Domain.Entities;

namespace Documentation.Application.Abstractions.Persistence;

public interface IDocumentationVersionRepository
{
    Task<DocumentationVersion?> GetByIdAsync(Guid documentationId, Guid versionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid documentationId, string version, string environment, CancellationToken cancellationToken = default);

    void Add(DocumentationVersion version);
}
