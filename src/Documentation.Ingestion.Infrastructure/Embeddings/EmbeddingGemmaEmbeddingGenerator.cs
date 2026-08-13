using System.Diagnostics;
using Documentation.Embeddings.Grpc;
using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Exceptions;
using Grpc.Core;

namespace Documentation.Ingestion.Infrastructure.Embeddings;

public sealed class EmbeddingGemmaEmbeddingGenerator(
    EmbeddingService.EmbeddingServiceClient client) : IEmbeddingGenerator
{
    public const int RequiredDimensions = 768;

    public int Dimensions => RequiredDimensions;

    public async Task<float[]> GenerateAsync(string content, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = Activity.Current?.GetBaggageItem("CorrelationId");
            Metadata? headers = correlationId is null
                ? null
                : new Metadata { { "x-correlation-id", correlationId } };
            var response = await client.EmbedDocumentAsync(
                new EmbedRequest { Text = content },
                headers: headers,
                deadline: DateTime.UtcNow.AddSeconds(100),
                cancellationToken: cancellationToken);

            if (response.Embedding.Count != RequiredDimensions)
            {
                throw new PermanentIngestionException(
                    $"EmbeddingGemma must return exactly {RequiredDimensions} dimensions.");
            }

            return response.Embedding.ToArray();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.InvalidArgument)
        {
            throw new PermanentIngestionException("EmbeddingGemma rejected the document.");
        }
        catch (RpcException exception)
        {
            throw new InvalidOperationException(
                $"EmbeddingGemma failed with gRPC status {exception.StatusCode}.");
        }
    }
}
