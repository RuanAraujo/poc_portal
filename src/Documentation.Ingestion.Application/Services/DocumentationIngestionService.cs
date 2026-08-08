using Documentation.Contracts;
using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Models;
using Documentation.Ingestion.Domain.Entities;

namespace Documentation.Ingestion.Application.Services;

public sealed class DocumentationIngestionService : IIngestionService
{
    private const int RequiredEmbeddingDimensions = 1024;

    private readonly IChunkRepository _chunkRepository;
    private readonly IDocumentationApiClient _documentationApiClient;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IIngestionUnitOfWork _unitOfWork;
    private readonly IOpenApiChunker _openApiChunker;
    private readonly IProcessedIntegrationEventRepository _processedEventRepository;

    public DocumentationIngestionService(
        IChunkRepository chunkRepository,
        IDocumentationApiClient documentationApiClient,
        IEmbeddingGenerator embeddingGenerator,
        IIngestionUnitOfWork unitOfWork,
        IOpenApiChunker openApiChunker,
        IProcessedIntegrationEventRepository processedEventRepository)
    {
        _chunkRepository = chunkRepository;
        _documentationApiClient = documentationApiClient;
        _embeddingGenerator = embeddingGenerator;
        _unitOfWork = unitOfWork;
        _openApiChunker = openApiChunker;
        _processedEventRepository = processedEventRepository;
    }

    public async Task<IngestionOutcome> ProcessAsync(
        DocumentationPublished integrationEvent,
        CancellationToken cancellationToken)
    {
        if (await _processedEventRepository.ExistsAsync(integrationEvent.EventId, cancellationToken))
        {
            await _documentationApiClient.UpdateIndexingStatusAsync(
                integrationEvent.DocumentId,
                integrationEvent.VersionId,
                DocumentationIndexingStatus.Available,
                cancellationToken);

            return IngestionOutcome.AlreadyProcessed();
        }

        var documentation = await _documentationApiClient.GetContentAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            cancellationToken);

        await _documentationApiClient.UpdateIndexingStatusAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            DocumentationIndexingStatus.Indexing,
            cancellationToken);

        var drafts = _openApiChunker.CreateChunks(documentation);
        var chunks = new List<DocumentChunk>(drafts.Count);

        foreach (var draft in drafts)
        {
            var embedding = await _embeddingGenerator.GenerateAsync(draft.Content, cancellationToken);

            if (embedding.Length != RequiredEmbeddingDimensions)
            {
                throw new InvalidOperationException(
                    $"The embedding provider returned {embedding.Length} dimensions; {RequiredEmbeddingDimensions} are required.");
            }

            chunks.Add(DocumentChunk.Create(
                integrationEvent.DocumentId,
                integrationEvent.VersionId,
                draft.ChunkIndex,
                draft.ChunkType,
                draft.Content,
                draft.MetadataJson,
                embedding));
        }

        await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await _chunkRepository.ReplaceForVersionAsync(
                integrationEvent.DocumentId,
                integrationEvent.VersionId,
                chunks,
                transactionCancellationToken);

            await _processedEventRepository.AddAsync(
                ProcessedIntegrationEvent.Create(integrationEvent.EventId),
                transactionCancellationToken);
        }, cancellationToken);

        await _documentationApiClient.UpdateIndexingStatusAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            DocumentationIndexingStatus.Available,
            cancellationToken);

        return IngestionOutcome.Processed(chunks.Count);
    }

    public Task MarkIndexingFailedAsync(
        DocumentationPublished integrationEvent,
        CancellationToken cancellationToken) =>
        _documentationApiClient.UpdateIndexingStatusAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            DocumentationIndexingStatus.IndexingFailed,
            cancellationToken);
}
