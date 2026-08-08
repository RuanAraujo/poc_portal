using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Documentation.Ingestion.Application.Abstractions;
using Documentation.Ingestion.Application.Exceptions;
using Documentation.Ingestion.Application.Models;

namespace Documentation.Ingestion.Infrastructure.Clients;

public sealed class DocumentationApiClient : IDocumentationApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public DocumentationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DocumentationContent> GetContentAsync(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"internal/documentations/{documentId}/versions/{versionId}/content",
            cancellationToken);

        await EnsureSuccessAsync(response, isContentRequest: true, cancellationToken);

        var content = await response.Content.ReadFromJsonAsync<DocumentationContent>(JsonOptions, cancellationToken);
        if (content is null || string.IsNullOrWhiteSpace(content.Content))
        {
            throw new PermanentIngestionException("The documentation API returned an empty content payload.");
        }

        return content;
    }

    public async Task UpdateIndexingStatusAsync(
        Guid documentId,
        Guid versionId,
        DocumentationIndexingStatus status,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"internal/documentations/{documentId}/versions/{versionId}/indexing-status",
            new UpdateIndexingStatusRequest(status),
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, isContentRequest: false, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        bool isContentRequest,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = $"Documentation API returned {(int)response.StatusCode} ({response.ReasonPhrase}): {responseBody}";

        if (response.StatusCode != HttpStatusCode.TooManyRequests &&
            (isContentRequest || (int)response.StatusCode is >= 400 and < 500))
        {
            throw new PermanentIngestionException(message);
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private sealed record UpdateIndexingStatusRequest(DocumentationIndexingStatus Status);
}
