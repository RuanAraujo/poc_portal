using System.Security.Cryptography;
using System.Text;

namespace Documentation.Ingestion.Domain.Entities;

public sealed class DocumentChunk
{
    private DocumentChunk()
    {
        Content = string.Empty;
        ChunkType = string.Empty;
        ContentHash = string.Empty;
        MetadataJson = "{}";
        Embedding = [];
    }

    private DocumentChunk(
        Guid documentId,
        Guid versionId,
        int chunkIndex,
        string chunkType,
        string content,
        string metadataJson,
        float[] embedding)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        VersionId = versionId;
        ChunkIndex = chunkIndex;
        ChunkType = chunkType;
        Content = content;
        ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        MetadataJson = metadataJson;
        Embedding = embedding;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public Guid VersionId { get; private set; }

    public int ChunkIndex { get; private set; }

    public string ChunkType { get; private set; }

    public string Content { get; private set; }

    public string ContentHash { get; private set; }

    public string MetadataJson { get; private set; }

    public float[] Embedding { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static DocumentChunk Create(
        Guid documentId,
        Guid versionId,
        int chunkIndex,
        string chunkType,
        string content,
        string metadataJson,
        float[] embedding) =>
        new(documentId, versionId, chunkIndex, chunkType, content, metadataJson, embedding);
}
