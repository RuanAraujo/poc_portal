namespace Documentation.Contracts;

public sealed record DocumentationPublished(
    Guid EventId,
    string EventType,
    Guid DocumentId,
    Guid VersionId,
    string ApiId,
    string Version,
    string Environment,
    DateTimeOffset OccurredAt)
{
    public const string EventName = "DocumentationPublished";
}
