using System.Text.Json;
using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Exceptions;
using Documentation.Ingestion.Application.Models;
using Documentation.Ingestion.Domain.ValueObjects;
using YamlDotNet.Serialization;

namespace Documentation.Ingestion.Infrastructure.OpenApi;

public sealed class OpenApiChunker : IOpenApiChunker
{
    private static readonly string[] HttpMethods =
    [
        "get", "put", "post", "delete", "options", "head", "patch", "trace"
    ];

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();

    public IReadOnlyList<DocumentChunkDraft> CreateChunks(DocumentationContent documentation)
    {
        try
        {
            using var source = Parse(documentation);
            var root = source.RootElement;
            var chunks = new List<DocumentChunkDraft>
            {
                new(
                    0,
                    "document",
                    CreateDocumentChunkContent(root, documentation.Content),
                    JsonSerializer.Serialize(new { kind = "document", format = documentation.Format }))
            };

            if (!root.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
            {
                return chunks;
            }

            var chunkIndex = 1;
            foreach (var path in paths.EnumerateObject())
            {
                if (path.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var method in HttpMethods)
                {
                    if (!path.Value.TryGetProperty(method, out var operation) ||
                        operation.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    chunks.Add(new DocumentChunkDraft(
                        chunkIndex++,
                        "operation",
                        CreateOperationChunkContent(path.Name, method, operation),
                        JsonSerializer.Serialize(new
                        {
                            kind = "operation",
                            format = documentation.Format,
                            path = path.Name,
                            method,
                            operationId = GetString(operation, "operationId")
                        })));
                }
            }

            return chunks;
        }
        catch (PermanentIngestionException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or YamlDotNet.Core.YamlException)
        {
            throw new PermanentIngestionException("The documentation content is not valid OpenAPI JSON or YAML.", exception);
        }
    }

    private JsonDocument Parse(DocumentationContent documentation)
    {
        if (string.Equals(documentation.Format, "yaml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(documentation.Format, "yml", StringComparison.OrdinalIgnoreCase))
        {
            var yamlObject = _yamlDeserializer.Deserialize<object>(documentation.Content);
            return JsonDocument.Parse(JsonSerializer.Serialize(yamlObject));
        }

        return JsonDocument.Parse(documentation.Content);
    }

    private static string CreateDocumentChunkContent(JsonElement root, string originalContent)
    {
        var openApi = GetString(root, "openapi") ?? GetString(root, "swagger") ?? "unknown";
        var info = root.TryGetProperty("info", out var infoValue) && infoValue.ValueKind == JsonValueKind.Object
            ? infoValue
            : default;
        var title = info.ValueKind == JsonValueKind.Object ? GetString(info, "title") : null;
        var version = info.ValueKind == JsonValueKind.Object ? GetString(info, "version") : null;
        var description = info.ValueKind == JsonValueKind.Object ? GetString(info, "description") : null;

        return $"OpenAPI documentation\n" +
               $"Specification: {openApi}\n" +
               $"Title: {title ?? "unknown"}\n" +
               $"Version: {version ?? "unknown"}\n" +
               $"Description: {description ?? string.Empty}\n\n" +
               originalContent;
    }

    private static string CreateOperationChunkContent(string path, string method, JsonElement operation)
    {
        var summary = GetString(operation, "summary");
        var description = GetString(operation, "description");
        var operationId = GetString(operation, "operationId");
        var tags = operation.TryGetProperty("tags", out var tagsValue) && tagsValue.ValueKind == JsonValueKind.Array
            ? string.Join(", ", tagsValue.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()))
            : string.Empty;

        return $"Operation: {method.ToUpperInvariant()} {path}\n" +
               $"OperationId: {operationId ?? string.Empty}\n" +
               $"Summary: {summary ?? string.Empty}\n" +
               $"Description: {description ?? string.Empty}\n" +
               $"Tags: {tags}\n\n" +
               operation.GetRawText();
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
