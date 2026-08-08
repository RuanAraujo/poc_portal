using Documentation.Domain.Enums;

namespace Documentation.Api.Contracts;

public sealed record CreateDocumentationRequest(
    string ApiId,
    string Name,
    string Version,
    string Environment,
    string Format,
    string Content);

public sealed record UpdateIndexingStatusRequest(
    DocumentationVersionStatus Status,
    string? Error);
