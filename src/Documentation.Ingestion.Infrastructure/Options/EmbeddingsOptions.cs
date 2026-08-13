namespace Documentation.Ingestion.Infrastructure.Options;

public sealed class EmbeddingsOptions
{
    public const string SectionName = "Embeddings";

    public string BaseUrl { get; init; } = "http://localhost:8080";

    public int Dimensions { get; init; } = 768;
}
