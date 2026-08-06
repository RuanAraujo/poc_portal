namespace Documentation.Ingestion.Domain.Entities;

public sealed class ProcessedIntegrationEvent
{
    private ProcessedIntegrationEvent()
    {
    }

    private ProcessedIntegrationEvent(Guid eventId)
    {
        EventId = eventId;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid EventId { get; private set; }

    public DateTimeOffset ProcessedAtUtc { get; private set; }

    public static ProcessedIntegrationEvent Create(Guid eventId) => new(eventId);
}
