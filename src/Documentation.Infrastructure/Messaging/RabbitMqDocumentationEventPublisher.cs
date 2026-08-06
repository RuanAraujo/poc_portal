using System.Text.Json;
using Documentation.Application.Abstractions.Messaging;
using Documentation.Contracts;
using RabbitMQ.Client;

namespace Documentation.Infrastructure.Messaging;

public sealed class RabbitMqDocumentationEventPublisher(RabbitMqOptions options) : IDocumentationEventPublisher
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
        properties.Timestamp = new AmqpTimestamp(integrationEvent.OccurredAt.ToUnixTimeSeconds());

        var payload = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, SerializerOptions);
        channel.BasicPublish(
            RabbitMqTopology.DocumentationExchange,
            RabbitMqTopology.PublishedRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: payload);

        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
}
