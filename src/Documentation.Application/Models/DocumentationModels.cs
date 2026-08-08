using Documentation.Domain.Enums;

namespace Documentation.Application.Models;

public sealed record CreateDocumentationCommand(
    string ApiId,
    string Name,
    string Version,
    string Environment,
    string Format,
    string Content);

public sealed record DocumentationVersionSummary(
    Guid Id,
    string Version,
    string Environment,
    string Format,
    DocumentationVersionStatus Status,
    string? LastError,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? IndexingUpdatedAtUtc);

public sealed record DocumentationSummary(
    Guid Id,
    string ApiId,
    string Name,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<DocumentationVersionSummary> Versions);

public sealed record DocumentationContent(
    Guid DocumentId,
    Guid VersionId,
    string Format,
    string Content,
    string ApiId,
    string Version,
    string Environment);

public enum PublishAttemptResultKind
{
    Accepted,
    Conflict,
    NotFound,
    PublishFailed
}

public sealed record PublishAttemptResult(
    PublishAttemptResultKind Kind,
    Guid? DocumentId = null,
    Guid? VersionId = null,
    DocumentationVersionStatus? Status = null,
    string? Error = null);
