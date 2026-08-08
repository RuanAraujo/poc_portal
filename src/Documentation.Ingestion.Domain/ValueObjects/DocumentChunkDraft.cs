namespace Documentation.Ingestion.Domain.ValueObjects;

public sealed record DocumentChunkDraft(
    int ChunkIndex,
    string ChunkType,
    string Content,
    string MetadataJson);
