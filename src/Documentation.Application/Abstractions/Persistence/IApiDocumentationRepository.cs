using Documentation.Domain.Entities;

namespace Documentation.Application.Abstractions.Persistence;

public interface IApiDocumentationRepository
{
    Task<ApiDocumentation?> GetByApiIdAsync(string apiId, CancellationToken cancellationToken = default);

    Task<ApiDocumentation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApiDocumentation>> ListAsync(CancellationToken cancellationToken = default);

    void Add(ApiDocumentation documentation);
}
