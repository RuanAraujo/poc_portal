namespace Documentation.Ingestion.Infrastructure.Options;

public sealed class EmbeddingsOptions
{
    public const string SectionName = "Embeddings";

    public string Provider { get; init; } = "Fake";

    public int Dimensions { get; init; } = 1024;

    public string BedrockRegion { get; init; } = "us-east-1";

    public string BedrockModelId { get; init; } = "amazon.titan-embed-text-v2:0";
}
