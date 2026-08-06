using Documentation.Domain.Enums;

namespace Documentation.Api.Contracts;

public sealed record PublishDocumentationResponse(
    Guid DocumentId,
    Guid VersionId,
    DocumentationVersionStatus Status,
    string? Error = null);
