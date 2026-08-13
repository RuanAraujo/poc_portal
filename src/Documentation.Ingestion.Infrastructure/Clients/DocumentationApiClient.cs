using System.Diagnostics;
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
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"internal/documentations/{documentId}/versions/{versionId}/content");
        AddCorrelationId(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        EnsureSuccess(response, isContentRequest: true);

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
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"internal/documentations/{documentId}/versions/{versionId}/indexing-status")
        {
            Content = JsonContent.Create(new UpdateIndexingStatusRequest(status), options: JsonOptions)
        };
        AddCorrelationId(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        EnsureSuccess(response, isContentRequest: false);
    }

    private static void EnsureSuccess(
        HttpResponseMessage response,
        bool isContentRequest)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = $"Documentation API returned HTTP {(int)response.StatusCode}.";

        if (response.StatusCode != HttpStatusCode.TooManyRequests &&
            (isContentRequest || (int)response.StatusCode is >= 400 and < 500))
        {
            throw new PermanentIngestionException(message);
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static void AddCorrelationId(HttpRequestMessage request)
    {
        var correlationId = Activity.Current?.GetBaggageItem("CorrelationId");
        if (correlationId is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
        }
    }

    private sealed record UpdateIndexingStatusRequest(DocumentationIndexingStatus Status);
}
