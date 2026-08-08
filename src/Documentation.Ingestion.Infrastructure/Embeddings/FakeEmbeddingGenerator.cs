using System.Security.Cryptography;
using System.Text;
using Documentation.Ingestion.Application.Abstractions;

namespace Documentation.Ingestion.Infrastructure.Embeddings;

public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
{
    public const int DefaultDimensions = 1024;

    public int Dimensions => DefaultDimensions;

    public Task<float[]> GenerateAsync(string content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var seedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var state = BitConverter.ToUInt32(seedBytes, 0);
        if (state == 0)
        {
            state = 0x6D2B79F5;
        }

        var embedding = new float[DefaultDimensions];
        double magnitudeSquared = 0;

        for (var index = 0; index < embedding.Length; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;

            var value = ((state / (double)uint.MaxValue) * 2d) - 1d;
            embedding[index] = (float)value;
            magnitudeSquared += value * value;
        }

        var magnitude = Math.Sqrt(magnitudeSquared);
        for (var index = 0; index < embedding.Length; index++)
        {
            embedding[index] = (float)(embedding[index] / magnitude);
        }

        return Task.FromResult(embedding);
    }
}
