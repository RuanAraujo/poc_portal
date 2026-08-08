using Documentation.Ingestion.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Documentation.Ingestion.Infrastructure.Persistence;

public sealed class IngestionUnitOfWork : IIngestionUnitOfWork
{
    private readonly IngestionDbContext _dbContext;

    public IngestionUnitOfWork(IngestionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await operation(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
