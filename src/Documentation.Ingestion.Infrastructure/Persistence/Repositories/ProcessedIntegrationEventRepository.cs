using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Documentation.Ingestion.Infrastructure.Persistence.Repositories;

public sealed class ProcessedIntegrationEventRepository : IProcessedIntegrationEventRepository
{
    private readonly IngestionDbContext _dbContext;

    public ProcessedIntegrationEventRepository(IngestionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken) =>
        _dbContext.ProcessedIntegrationEvents
            .AsNoTracking()
            .AnyAsync(integrationEvent => integrationEvent.EventId == eventId, cancellationToken);

    public Task AddAsync(ProcessedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        _dbContext.ProcessedIntegrationEvents.AddAsync(integrationEvent, cancellationToken).AsTask();
}
