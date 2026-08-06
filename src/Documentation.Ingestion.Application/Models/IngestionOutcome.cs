namespace Documentation.Ingestion.Application.Models;

public sealed record IngestionOutcome(bool WasAlreadyProcessed, int ChunkCount)
{
    public static IngestionOutcome AlreadyProcessed() => new(true, 0);

    public static IngestionOutcome Processed(int chunkCount) => new(false, chunkCount);
}
