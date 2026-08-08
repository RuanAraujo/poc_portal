using Documentation.Contracts;
using Documentation.Ingestion.Application.Models;

namespace Documentation.Ingestion.Application.Services;

public interface IIngestionService
{
    Task<IngestionOutcome> ProcessAsync(
        DocumentationPublished integrationEvent,
        CancellationToken cancellationToken);

    Task MarkIndexingFailedAsync(
        DocumentationPublished integrationEvent,
        CancellationToken cancellationToken);
}
