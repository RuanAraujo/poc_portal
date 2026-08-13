using System.Diagnostics;
using Documentation.Contracts;
using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Models;
using Documentation.Ingestion.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Documentation.Ingestion.Application.Services;

public sealed class DocumentationIngestionService : IIngestionService
{
    private const int RequiredEmbeddingDimensions = 768;

    private readonly IChunkRepository _chunkRepository;
    private readonly IDocumentationApiClient _documentationApiClient;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IIngestionUnitOfWork _unitOfWork;
    private readonly IOpenApiChunker _openApiChunker;
    private readonly IProcessedIntegrationEventRepository _processedEventRepository;
    private readonly ILogger<DocumentationIngestionService> _logger;

    public DocumentationIngestionService(
        IChunkRepository chunkRepository,
        IDocumentationApiClient documentationApiClient,
        IEmbeddingGenerator embeddingGenerator,
        IIngestionUnitOfWork unitOfWork,
        IOpenApiChunker openApiChunker,
        IProcessedIntegrationEventRepository processedEventRepository,
        ILogger<DocumentationIngestionService> logger)
    {
        _chunkRepository = chunkRepository;
        _documentationApiClient = documentationApiClient;
        _embeddingGenerator = embeddingGenerator;
        _unitOfWork = unitOfWork;
        _openApiChunker = openApiChunker;
        _processedEventRepository = processedEventRepository;
        _logger = logger;
    }

    public async Task<IngestionOutcome> ProcessAsync(
        DocumentationPublished integrationEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Documentation ingestion started. Step: {Step}.", "Started");

        if (await _processedEventRepository.ExistsAsync(integrationEvent.EventId, cancellationToken))
        {
            _logger.LogInformation("Documentation event was already processed. Step: {Step}.", "Deduplicated");
            await _documentationApiClient.UpdateIndexingStatusAsync(
                integrationEvent.DocumentId,
                integrationEvent.VersionId,
                DocumentationIndexingStatus.Available,
                cancellationToken);
            _logger.LogInformation("Documentation indexing status updated. Status: {Status}.", DocumentationIndexingStatus.Available);

            return IngestionOutcome.AlreadyProcessed();
        }

        var documentation = await _documentationApiClient.GetContentAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            cancellationToken);
        _logger.LogInformation(
            "Documentation content retrieved. Format: {Format}; characters: {CharacterCount}.",
            documentation.Format,
            documentation.Content.Length);

        await _documentationApiClient.UpdateIndexingStatusAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            DocumentationIndexingStatus.Indexing,
            cancellationToken);
        _logger.LogInformation("Documentation indexing status updated. Status: {Status}.", DocumentationIndexingStatus.Indexing);

        var drafts = _openApiChunker.CreateChunks(documentation);
        _logger.LogInformation("Documentation chunks created. Chunk count: {ChunkCount}.", drafts.Count);
        var chunks = new List<DocumentChunk>(drafts.Count);
        var embeddingsStartedAt = Stopwatch.GetTimestamp();

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

        _logger.LogInformation(
            "Documentation embeddings generated. Chunk count: {ChunkCount}; dimensions: {EmbeddingDimensions}; elapsed: {ElapsedMs} ms.",
            chunks.Count,
            RequiredEmbeddingDimensions,
            (long)Stopwatch.GetElapsedTime(embeddingsStartedAt).TotalMilliseconds);

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
        _logger.LogInformation("Documentation chunks and event persisted. Chunk count: {ChunkCount}.", chunks.Count);

        await _documentationApiClient.UpdateIndexingStatusAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            DocumentationIndexingStatus.Available,
            cancellationToken);
        _logger.LogInformation("Documentation indexing status updated. Status: {Status}.", DocumentationIndexingStatus.Available);

        return IngestionOutcome.Processed(chunks.Count);
    }

    public async Task MarkIndexingFailedAsync(
        DocumentationPublished integrationEvent,
        CancellationToken cancellationToken)
    {
        await _documentationApiClient.UpdateIndexingStatusAsync(
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            DocumentationIndexingStatus.IndexingFailed,
            cancellationToken);
        _logger.LogWarning("Documentation indexing status updated. Status: {Status}.", DocumentationIndexingStatus.IndexingFailed);
    }
}
