using System.Diagnostics;
using System.Text.Json;
using Documentation.Application.Abstractions.Messaging;
using Documentation.Contracts;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Documentation.Infrastructure.Messaging;

public sealed class RabbitMqDocumentationEventPublisher(
    RabbitMqOptions options,
    ILogger<RabbitMqDocumentationEventPublisher> logger) : IDocumentationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ConnectionFactory _connectionFactory = new()
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        AutomaticRecoveryEnabled = true
    };

    public Task PublishAsync(DocumentationPublished integrationEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var correlationId = Activity.Current?.GetBaggageItem("CorrelationId")
            ?? integrationEvent.EventId.ToString("N");

        logger.LogInformation(
            "Publishing documentation event {EventId} for document {DocumentId}, version {VersionId}, routing key {RoutingKey}, step {Step}",
            integrationEvent.EventId,
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            RabbitMqTopology.PublishedRoutingKey,
            "RabbitMqPublish");

        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            RabbitMqTopology.DocumentationExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false);
        channel.QueueDeclare(
            RabbitMqTopology.IngestionQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        channel.QueueBind(
            RabbitMqTopology.IngestionQueue,
            RabbitMqTopology.DocumentationExchange,
            RabbitMqTopology.PublishedRoutingKey);
        channel.ConfirmSelect();

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = DocumentationPublished.EventName;
        properties.MessageId = integrationEvent.EventId.ToString();
        properties.CorrelationId = correlationId;
        properties.Timestamp = new AmqpTimestamp(integrationEvent.OccurredAt.ToUnixTimeSeconds());

        var payload = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, SerializerOptions);
        channel.BasicPublish(
            RabbitMqTopology.DocumentationExchange,
            RabbitMqTopology.PublishedRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: payload);

        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

        logger.LogInformation(
            "Documentation event {EventId} confirmed for document {DocumentId}, version {VersionId}, correlation {CorrelationId}, outcome {Outcome}",
            integrationEvent.EventId,
            integrationEvent.DocumentId,
            integrationEvent.VersionId,
            correlationId,
            "Confirmed");
        return Task.CompletedTask;
    }
}
