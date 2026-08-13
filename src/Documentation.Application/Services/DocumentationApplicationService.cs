using Documentation.Application.Abstractions.Messaging;
using Documentation.Application.Abstractions.Persistence;
using Documentation.Application.Models;
using Documentation.Contracts;
using Documentation.Domain.Entities;
using Documentation.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Documentation.Application.Services;

public sealed class DocumentationApplicationService(
    IApiDocumentationRepository documentationRepository,
    IDocumentationVersionRepository versionRepository,
    IUnitOfWork unitOfWork,
    IDocumentationEventPublisher eventPublisher,
    ILogger<DocumentationApplicationService> logger)
{
    public async Task<PublishAttemptResult> CreateAndPublishAsync(
        CreateDocumentationCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Creating documentation version {Version} for API {ApiId} in {Environment} as {Format} with {ContentLength} characters",
            command.Version,
            command.ApiId,
            command.Environment,
            command.Format,
            command.Content.Length);

        var documentation = await documentationRepository
            .GetByApiIdAsync(command.ApiId, cancellationToken);

        if (documentation is null)
        {
            documentation = new ApiDocumentation(
                Guid.NewGuid(),
                command.ApiId,
                command.Name,
                DateTimeOffset.UtcNow);
            documentationRepository.Add(documentation);
        }

        if (await versionRepository.ExistsAsync(documentation.Id, command.Version, command.Environment, cancellationToken))
        {
            logger.LogWarning(
                "Documentation version already exists for document {DocumentId}, version {Version}, environment {Environment}",
                documentation.Id,
                command.Version,
                command.Environment);
            return new PublishAttemptResult(PublishAttemptResultKind.Conflict, documentation.Id);
        }

        var documentationVersion = new DocumentationVersion(
            Guid.NewGuid(),
            documentation.Id,
            command.Version,
            command.Environment,
            command.Format,
            command.Content,
            DateTimeOffset.UtcNow);

        versionRepository.Add(documentationVersion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Documentation version persisted for document {DocumentId}, version {VersionId}, step {Step}, outcome {Outcome}",
            documentation.Id,
            documentationVersion.Id,
            "Persisted",
            "Succeeded");

        return await PublishVersionAsync(documentation, documentationVersion, cancellationToken);
    }

    public async Task<PublishAttemptResult> RepublishAsync(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Republishing documentation version for document {DocumentId}, version {VersionId}",
            documentId,
            versionId);

        var documentation = await documentationRepository.GetByIdAsync(documentId, cancellationToken);
        var documentationVersion = await versionRepository.GetByIdAsync(documentId, versionId, cancellationToken);

        if (documentation is null || documentationVersion is null)
        {
            logger.LogWarning(
                "Documentation version was not found for republish: document {DocumentId}, version {VersionId}",
                documentId,
                versionId);
            return new PublishAttemptResult(PublishAttemptResultKind.NotFound);
        }

        documentationVersion.MarkPublishing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Documentation version status updated for document {DocumentId}, version {VersionId}, status {Status}, step {Step}",
            documentId,
            versionId,
            documentationVersion.Status,
            "RepublishRequested");

        return await PublishVersionAsync(documentation, documentationVersion, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentationSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var documentations = await documentationRepository.ListAsync(cancellationToken);
        return documentations.Select(ToSummary).ToList();
    }

    public async Task<DocumentationSummary?> GetAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var documentation = await documentationRepository.GetByIdAsync(documentId, cancellationToken);
        return documentation is null ? null : ToSummary(documentation);
    }

    public async Task<DocumentationContent?> GetContentAsync(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var documentation = await documentationRepository.GetByIdAsync(documentId, cancellationToken);
        var documentationVersion = await versionRepository.GetByIdAsync(documentId, versionId, cancellationToken);

        if (documentation is null || documentationVersion is null)
        {
            logger.LogWarning(
                "Documentation content was not found for document {DocumentId}, version {VersionId}",
                documentId,
                versionId);
            return null;
        }

        logger.LogInformation(
            "Documentation content loaded for document {DocumentId}, version {VersionId}, content length {ContentLength}",
            documentId,
            versionId,
            documentationVersion.Content.Length);

        return new DocumentationContent(
            documentId,
            versionId,
            documentationVersion.Format,
            documentationVersion.Content,
            documentation.ApiId,
            documentationVersion.Version,
            documentationVersion.Environment);
    }

    public async Task<bool> UpdateIndexingStatusAsync(
        Guid documentId,
        Guid versionId,
        DocumentationVersionStatus status,
        string? error,
        CancellationToken cancellationToken = default)
    {
        if (status is not DocumentationVersionStatus.Indexing
            and not DocumentationVersionStatus.Available
            and not DocumentationVersionStatus.IndexingFailed)
        {
            logger.LogWarning(
                "Rejected indexing status {Status} for document {DocumentId}, version {VersionId}",
                status,
                documentId,
                versionId);
            return false;
        }

        var documentationVersion = await versionRepository.GetByIdAsync(documentId, versionId, cancellationToken);
        if (documentationVersion is null)
        {
            logger.LogWarning(
                "Documentation version was not found for indexing status update: document {DocumentId}, version {VersionId}",
                documentId,
                versionId);
            return false;
        }

        documentationVersion.UpdateIndexingStatus(status, DateTimeOffset.UtcNow, error);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Indexing status updated for document {DocumentId}, version {VersionId}, status {Status}, step {Step}, outcome {Outcome}",
            documentId,
            versionId,
            status,
            "IndexingStatusUpdated",
            "Succeeded");
        return true;
    }

    private async Task<PublishAttemptResult> PublishVersionAsync(
        ApiDocumentation documentation,
        DocumentationVersion documentationVersion,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new DocumentationPublished(
            Guid.NewGuid(),
            DocumentationPublished.EventName,
            documentation.Id,
            documentationVersion.Id,
            documentation.ApiId,
            documentationVersion.Version,
            documentationVersion.Environment,
            DateTimeOffset.UtcNow);

        logger.LogInformation(
            "Publishing documentation event {EventId} for document {DocumentId}, version {VersionId}, step {Step}",
            integrationEvent.EventId,
            documentation.Id,
            documentationVersion.Id,
            "Publish");

        try
        {
            await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            documentationVersion.MarkPendingIndexing(DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Documentation event {EventId} processed for document {DocumentId}, version {VersionId}, status {Status}, outcome {Outcome}",
                integrationEvent.EventId,
                documentation.Id,
                documentationVersion.Id,
                documentationVersion.Status,
                "Succeeded");

            return new PublishAttemptResult(
                PublishAttemptResultKind.Accepted,
                documentation.Id,
                documentationVersion.Id,
                documentationVersion.Status);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Documentation event {EventId} failed for document {DocumentId}, version {VersionId}, step {Step}, outcome {Outcome}",
                integrationEvent.EventId,
                documentation.Id,
                documentationVersion.Id,
                "Publish",
                "Failed");

            documentationVersion.MarkPublishFailed(exception.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new PublishAttemptResult(
                PublishAttemptResultKind.PublishFailed,
                documentation.Id,
                documentationVersion.Id,
                documentationVersion.Status,
                exception.Message);
        }
    }

    private static DocumentationSummary ToSummary(ApiDocumentation documentation) => new(
        documentation.Id,
        documentation.ApiId,
        documentation.Name,
        documentation.CreatedAtUtc,
        documentation.Versions
            .OrderByDescending(version => version.CreatedAtUtc)
            .Select(version => new DocumentationVersionSummary(
                version.Id,
                version.Version,
                version.Environment,
                version.Format,
                version.Status,
                version.LastError,
                version.CreatedAtUtc,
                version.PublishedAtUtc,
                version.IndexingUpdatedAtUtc))
            .ToList());
}
