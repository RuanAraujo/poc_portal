using Documentation.Api.Contracts;
using Documentation.Application.Models;
using Documentation.Application.Services;
using Documentation.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Documentation.Api.Controllers;

[ApiController]
[Route("internal/documentations")]
public sealed class InternalDocumentationsController(DocumentationApplicationService documentationService) : ControllerBase
{
    [HttpGet("{documentId:guid}/versions/{versionId:guid}/content")]
    [ProducesResponseType(typeof(DocumentationContent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentationContent>> GetContent(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var content = await documentationService.GetContentAsync(documentId, versionId, cancellationToken);
        return content is null ? NotFound() : Ok(content);
    }

    [HttpPut("{documentId:guid}/versions/{versionId:guid}/indexing-status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIndexingStatus(
        Guid documentId,
        Guid versionId,
        [FromBody] UpdateIndexingStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is not DocumentationVersionStatus.Indexing
            and not DocumentationVersionStatus.Available
            and not DocumentationVersionStatus.IndexingFailed)
        {
            return BadRequest(new { message = "Only indexing, available and indexingFailed statuses are accepted." });
        }

        var updated = await documentationService.UpdateIndexingStatusAsync(
            documentId,
            versionId,
            request.Status,
            request.Error,
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }
}
