namespace Documentation.Ingestion.Application.Abstractions;

public interface IEmbeddingGenerator
{
    int Dimensions { get; }

    Task<float[]> GenerateAsync(string content, CancellationToken cancellationToken);
}
