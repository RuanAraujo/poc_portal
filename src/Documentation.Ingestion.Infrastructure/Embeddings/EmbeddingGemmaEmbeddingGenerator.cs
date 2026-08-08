using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Exceptions;

namespace Documentation.Ingestion.Infrastructure.Embeddings;

public sealed class EmbeddingGemmaEmbeddingGenerator(HttpClient httpClient) : IEmbeddingGenerator
{
    public const int RequiredDimensions = 768;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public int Dimensions => RequiredDimensions;

    public async Task<float[]> GenerateAsync(string content, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "internal/embeddings",
            new EmbeddingRequest(content),
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = $"EmbeddingGemma service returned {(int)response.StatusCode} ({response.ReasonPhrase}): {responseBody}";

            if (response.StatusCode != HttpStatusCode.TooManyRequests &&
                (int)response.StatusCode is >= 400 and < 500)
            {
                throw new PermanentIngestionException(message);
            }

            throw new HttpRequestException(message, null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonOptions, cancellationToken);
        if (result is null || result.Dimensions != RequiredDimensions || result.Embedding.Length != RequiredDimensions)
        {
            throw new PermanentIngestionException(
                $"EmbeddingGemma must return exactly {RequiredDimensions} dimensions.");
        }

        return result.Embedding;
    }

    private sealed record EmbeddingRequest(string Text);

    private sealed record EmbeddingResponse(float[] Embedding, int Dimensions);
}
