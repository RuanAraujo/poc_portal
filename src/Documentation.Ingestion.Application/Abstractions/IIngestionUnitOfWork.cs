namespace Documentation.Ingestion.Application.Abstractions;

public interface IIngestionUnitOfWork
{
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
