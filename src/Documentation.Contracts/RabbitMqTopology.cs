namespace Documentation.Contracts;

public static class RabbitMqTopology
{
    public const string DocumentationExchange = "documentation.events";
    public const string PublishedRoutingKey = "documentation.published.v1";
    public const string IngestionQueue = "documentation.ingestion.v1";
    public const string RetryQueue = "documentation.ingestion.retry.v1";
    public const string DeadLetterQueue = "documentation.ingestion.dlq.v1";
}
