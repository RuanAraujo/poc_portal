using Documentation.Application.Abstractions.Persistence;
using Documentation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Documentation.Infrastructure.Persistence.Repositories;

public sealed class DocumentationVersionRepository(DocumentationDbContext dbContext) : IDocumentationVersionRepository
{
    public Task<DocumentationVersion?> GetByIdAsync(
        Guid documentationId,
        Guid versionId,
        CancellationToken cancellationToken = default) =>
        dbContext.DocumentationVersions.SingleOrDefaultAsync(
            version => version.DocumentationId == documentationId && version.Id == versionId,
            cancellationToken);

    public Task<bool> ExistsAsync(
        Guid documentationId,
        string version,
        string environment,
        CancellationToken cancellationToken = default) =>
        dbContext.DocumentationVersions.AnyAsync(
            documentationVersion => documentationVersion.DocumentationId == documentationId
                && documentationVersion.Version == version
                && documentationVersion.Environment == environment,
            cancellationToken);

    public void Add(DocumentationVersion version) => dbContext.DocumentationVersions.Add(version);
}
