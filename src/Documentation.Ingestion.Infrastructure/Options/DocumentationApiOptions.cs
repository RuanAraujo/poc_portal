namespace Documentation.Ingestion.Infrastructure.Options;

public sealed class DocumentationApiOptions
{
    public const string SectionName = "DocumentationApi";

    public string BaseUrl { get; init; } = "http://localhost:8080";
}
