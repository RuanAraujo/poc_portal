using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Exceptions;
using Documentation.Ingestion.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Documentation.Ingestion.Infrastructure.Embeddings;

public sealed class BedrockEmbeddingGenerator : IEmbeddingGenerator, IDisposable
{
    private const int RequiredDimensions = 1024;

    private readonly AmazonBedrockRuntimeClient _client;
    private readonly EmbeddingsOptions _options;

    public BedrockEmbeddingGenerator(IOptions<EmbeddingsOptions> options)
    {
        _options = options.Value;
        if (_options.Dimensions != RequiredDimensions)
        {
            throw new InvalidOperationException($"Bedrock requires {RequiredDimensions} dimensions for this POC.");
        }

        _client = new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(_options.BedrockRegion));
    }

    public int Dimensions => RequiredDimensions;

    public async Task<float[]> GenerateAsync(string content, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            inputText = content,
            dimensions = RequiredDimensions,
            normalize = true
        });

        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var response = await _client.InvokeModelAsync(new InvokeModelRequest
        {
            ModelId = _options.BedrockModelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = body
        }, cancellationToken);

        using var responseDocument = await JsonDocument.ParseAsync(response.Body, cancellationToken: cancellationToken);
        if (!responseDocument.RootElement.TryGetProperty("embedding", out var embeddingElement) ||
            embeddingElement.ValueKind != JsonValueKind.Array)
        {
            throw new PermanentIngestionException("Amazon Bedrock returned a response without an embedding array.");
        }

        var embedding = embeddingElement.EnumerateArray().Select(element => element.GetSingle()).ToArray();
        if (embedding.Length != RequiredDimensions)
        {
            throw new PermanentIngestionException(
                $"Amazon Bedrock returned {embedding.Length} dimensions; {RequiredDimensions} are required.");
        }

        return embedding;
    }

    public void Dispose() => _client.Dispose();
}
