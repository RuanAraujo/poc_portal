using Documentation.Domain.Enums;

namespace Documentation.Domain.Entities;

public sealed class DocumentationVersion
{
    private DocumentationVersion()
    {
        // Required by Entity Framework Core.
    }

    public DocumentationVersion(
        Guid id,
        Guid documentationId,
        string version,
        string environment,
        string format,
        string content,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        DocumentationId = documentationId;
        Version = version;
        Environment = environment;
        Format = format;
        Content = content;
        CreatedAtUtc = createdAtUtc;
        Status = DocumentationVersionStatus.Publishing;
    }

    public Guid Id { get; private set; }

    public Guid DocumentationId { get; private set; }

    public string Version { get; private set; } = string.Empty;

    public string Environment { get; private set; } = string.Empty;

    public string Format { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DocumentationVersionStatus Status { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public DateTimeOffset? IndexingUpdatedAtUtc { get; private set; }

    public ApiDocumentation? Documentation { get; private set; }

    public void MarkPublishing()
    {
        Status = DocumentationVersionStatus.Publishing;
        LastError = null;
        IndexingUpdatedAtUtc = null;
    }

    public void MarkPendingIndexing(DateTimeOffset publishedAtUtc)
    {
        Status = DocumentationVersionStatus.PendingIndexing;
        PublishedAtUtc = publishedAtUtc;
        LastError = null;
    }

    public void MarkPublishFailed(string error)
    {
        Status = DocumentationVersionStatus.PublishFailed;
        LastError = error;
    }

    public void UpdateIndexingStatus(DocumentationVersionStatus status, DateTimeOffset updatedAtUtc, string? error = null)
    {
        if (status is not DocumentationVersionStatus.Indexing
            and not DocumentationVersionStatus.Available
            and not DocumentationVersionStatus.IndexingFailed)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Only indexing states can be reported by the ingestion worker.");
        }

        Status = status;
        IndexingUpdatedAtUtc = updatedAtUtc;
        LastError = status == DocumentationVersionStatus.IndexingFailed ? error : null;
    }
}
