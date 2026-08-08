namespace Documentation.Ingestion.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    public int RetryDelaySeconds { get; init; } = 10;

    public int MaxRetryCount { get; init; } = 3;
}
