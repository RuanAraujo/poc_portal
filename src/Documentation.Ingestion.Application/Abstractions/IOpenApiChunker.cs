using Documentation.Ingestion.Application.Models;
using Documentation.Ingestion.Domain.ValueObjects;

namespace Documentation.Ingestion.Application.Abstractions;

public interface IOpenApiChunker
{
    IReadOnlyList<DocumentChunkDraft> CreateChunks(DocumentationContent documentation);
}
