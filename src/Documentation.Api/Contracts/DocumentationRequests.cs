using System.ComponentModel.DataAnnotations;
using Documentation.Domain.Enums;

namespace Documentation.Api.Contracts;

public sealed record CreateDocumentationRequest(
    [Required, MaxLength(200)] string ApiId,
    [Required, MaxLength(300)] string Name,
    [Required, MaxLength(100)] string Version,
    [Required, MaxLength(100)] string Environment,
    [Required, MaxLength(50)] string Format,
    [Required] string Content);

public sealed record UpdateIndexingStatusRequest(
    DocumentationVersionStatus Status,
    string? Error);
