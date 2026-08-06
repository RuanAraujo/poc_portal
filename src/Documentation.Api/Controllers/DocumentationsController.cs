using Documentation.Api.Contracts;
using Documentation.Application.Models;
using Documentation.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Documentation.Api.Controllers;

[ApiController]
[Route("api/documentations")]
public sealed class DocumentationsController(DocumentationApplicationService documentationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PublishDocumentationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(PublishDocumentationResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await documentationService.CreateAndPublishAsync(
            new CreateDocumentationCommand(
                request.ApiId,
                request.Name,
                request.Version,
                request.Environment,
                request.Format,
                request.Content),
            cancellationToken);

        return ToPublishActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentationSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentationSummary>>> List(CancellationToken cancellationToken) =>
        Ok(await documentationService.ListAsync(cancellationToken));

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(typeof(DocumentationSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentationSummary>> Get(Guid documentId, CancellationToken cancellationToken)
    {
        var documentation = await documentationService.GetAsync(documentId, cancellationToken);
        return documentation is null ? NotFound() : Ok(documentation);
    }

    [HttpPost("{documentId:guid}/versions/{versionId:guid}/republish")]
    [ProducesResponseType(typeof(PublishDocumentationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PublishDocumentationResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Republish(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await documentationService.RepublishAsync(documentId, versionId, cancellationToken);
        return ToPublishActionResult(result);
    }

    private IActionResult ToPublishActionResult(PublishAttemptResult result) => result.Kind switch
    {
        PublishAttemptResultKind.Accepted => AcceptedAtAction(
            nameof(Get),
            new { documentId = result.DocumentId },
            new PublishDocumentationResponse(
                result.DocumentId!.Value,
                result.VersionId!.Value,
                result.Status!.Value)),
        PublishAttemptResultKind.Conflict => Conflict(new
        {
            message = "A documentation version with the same version and environment already exists.",
            documentId = result.DocumentId
        }),
        PublishAttemptResultKind.NotFound => NotFound(),
        PublishAttemptResultKind.PublishFailed => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new PublishDocumentationResponse(
                result.DocumentId!.Value,
                result.VersionId!.Value,
                result.Status!.Value,
                result.Error)),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
