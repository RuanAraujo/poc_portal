using Documentation.Application.Abstractions.Persistence;
using Documentation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Documentation.Infrastructure.Persistence.Repositories;

public sealed class ApiDocumentationRepository(DocumentationDbContext dbContext) : IApiDocumentationRepository
{
    public Task<ApiDocumentation?> GetByApiIdAsync(string apiId, CancellationToken cancellationToken = default) =>
        dbContext.ApiDocumentations
            .Include(documentation => documentation.Versions)
            .SingleOrDefaultAsync(documentation => documentation.ApiId == apiId, cancellationToken);

    public Task<ApiDocumentation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ApiDocumentations
            .Include(documentation => documentation.Versions)
            .SingleOrDefaultAsync(documentation => documentation.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ApiDocumentation>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ApiDocumentations
            .Include(documentation => documentation.Versions)
            .OrderBy(documentation => documentation.Name)
            .ToListAsync(cancellationToken);

    public void Add(ApiDocumentation documentation) => dbContext.ApiDocumentations.Add(documentation);
}
