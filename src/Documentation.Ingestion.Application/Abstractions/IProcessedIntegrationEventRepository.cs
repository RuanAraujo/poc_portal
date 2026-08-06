using Documentation.Ingestion.Domain.Entities;

namespace Documentation.Ingestion.Application.Abstractions;

public interface IProcessedIntegrationEventRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken);

    Task AddAsync(ProcessedIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
