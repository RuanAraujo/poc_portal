using System.Text.Json;
using Documentation.Contracts;
using Documentation.Ingestion.Application.Exceptions;
using Documentation.Ingestion.Application.Services;
using Documentation.Ingestion.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Documentation.Ingestion.Worker;

public sealed class RabbitMqIngestionWorker : BackgroundService
{
    private const string RetryCountHeader = "x-retry-count";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<RabbitMqIngestionWorker> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public RabbitMqIngestionWorker(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqIngestionWorker> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilStoppedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "RabbitMQ consumer stopped unexpectedly. Reconnecting in five seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeUntilStoppedAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        using var connection = factory.CreateConnection("documentation-ingestion-worker");
        using var channel = connection.CreateModel();

        DeclareTopology(channel);
        channel.ConfirmSelect();
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);
        var consumerTag = channel.BasicConsume(
            queue: RabbitMqTopology.IngestionQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "RabbitMQ ingestion worker is live. Queue: {Queue}; retry queue: {RetryQueue}; DLQ: {DeadLetterQueue}",
            RabbitMqTopology.IngestionQueue,
            RabbitMqTopology.RetryQueue,
            RabbitMqTopology.DeadLetterQueue);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        finally
        {
            if (channel.IsOpen)
            {
                channel.BasicCancel(consumerTag);
            }
        }
    }

    private async Task HandleDeliveryAsync(
        IModel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        DocumentationPublished? integrationEvent = null;

        try
        {
            integrationEvent = JsonSerializer.Deserialize<DocumentationPublished>(delivery.Body.Span, JsonOptions);
            if (integrationEvent is null)
            {
                throw new PermanentIngestionException("The RabbitMQ message could not be deserialized as DocumentationPublished.");
            }

            if (!string.Equals(integrationEvent.EventType, DocumentationPublished.EventName, StringComparison.Ordinal))
            {
                throw new PermanentIngestionException($"Unsupported event type '{integrationEvent.EventType}'.");
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            var outcome = await ingestionService.ProcessAsync(integrationEvent, stoppingToken);

            channel.BasicAck(delivery.DeliveryTag, multiple: false);
            _logger.LogInformation(
                "Documentation event {EventId} acknowledged. Already processed: {AlreadyProcessed}; chunks: {ChunkCount}.",
                integrationEvent.EventId,
                outcome.WasAlreadyProcessed,
                outcome.ChunkCount);
        }
        catch (PermanentIngestionException exception)
        {
            await MarkFailedBestEffortAsync(integrationEvent, stoppingToken);
            PublishToDeadLetter(channel, delivery, exception.Message);
            channel.BasicAck(delivery.DeliveryTag, multiple: false);
            _logger.LogWarning(exception, "Documentation message moved to the DLQ due to a permanent failure.");
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            var currentRetryCount = GetRetryCount(delivery.BasicProperties.Headers);
            if (currentRetryCount < _options.MaxRetryCount)
            {
                PublishToRetry(channel, delivery, currentRetryCount + 1, exception.Message);
                channel.BasicAck(delivery.DeliveryTag, multiple: false);
                _logger.LogWarning(
                    exception,
                    "Documentation message will retry in {DelaySeconds}s. Retry {RetryCount}/{MaxRetryCount}.",
                    _options.RetryDelaySeconds,
                    currentRetryCount + 1,
                    _options.MaxRetryCount);
                return;
            }

            await MarkFailedBestEffortAsync(integrationEvent, stoppingToken);
            PublishToDeadLetter(channel, delivery, exception.Message);
            channel.BasicAck(delivery.DeliveryTag, multiple: false);
            _logger.LogError(
                exception,
                "Documentation message exhausted retries and was moved to the DLQ. Retry count: {RetryCount}.",
                currentRetryCount);
        }
    }

    private async Task MarkFailedBestEffortAsync(
        DocumentationPublished? integrationEvent,
        CancellationToken stoppingToken)
    {
        if (integrationEvent is null)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            await ingestionService.MarkIndexingFailedAsync(integrationEvent, stoppingToken);
        }
        catch (Exception callbackException) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                callbackException,
                "Could not report IndexingFailed for documentation version {VersionId}.",
                integrationEvent.VersionId);
        }
    }

    private void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(
            exchange: RabbitMqTopology.DocumentationExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: RabbitMqTopology.IngestionQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        channel.QueueBind(
            queue: RabbitMqTopology.IngestionQueue,
            exchange: RabbitMqTopology.DocumentationExchange,
            routingKey: RabbitMqTopology.PublishedRoutingKey);

        var retryArguments = new Dictionary<string, object>
        {
            ["x-message-ttl"] = _options.RetryDelaySeconds * 1000,
            ["x-dead-letter-exchange"] = RabbitMqTopology.DocumentationExchange,
            ["x-dead-letter-routing-key"] = RabbitMqTopology.PublishedRoutingKey
        };
        channel.QueueDeclare(
            queue: RabbitMqTopology.RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArguments);

        channel.QueueDeclare(
            queue: RabbitMqTopology.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
    }

    private void PublishToRetry(
        IModel channel,
        BasicDeliverEventArgs delivery,
        int retryCount,
        string errorMessage) =>
        PublishCopy(
            channel,
            exchange: string.Empty,
            routingKey: RabbitMqTopology.RetryQueue,
            delivery,
            retryCount,
            errorMessage);

    private void PublishToDeadLetter(
        IModel channel,
        BasicDeliverEventArgs delivery,
        string errorMessage) =>
        PublishCopy(
            channel,
            exchange: string.Empty,
            routingKey: RabbitMqTopology.DeadLetterQueue,
            delivery,
            GetRetryCount(delivery.BasicProperties.Headers),
            errorMessage);

    private static void PublishCopy(
        IModel channel,
        string exchange,
        string routingKey,
        BasicDeliverEventArgs delivery,
        int retryCount,
        string errorMessage)
    {
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = delivery.BasicProperties.MessageId;
        properties.CorrelationId = delivery.BasicProperties.CorrelationId;
        properties.Headers = delivery.BasicProperties.Headers is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(delivery.BasicProperties.Headers);
        properties.Headers[RetryCountHeader] = retryCount;
        properties.Headers["x-ingestion-last-error"] = errorMessage;

        channel.BasicPublish(exchange, routingKey, mandatory: true, basicProperties: properties, body: delivery.Body);
        if (!channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("RabbitMQ did not confirm the republished message.");
        }
    }

    private static int GetRetryCount(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue(RetryCountHeader, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte byteValue => byteValue,
            short shortValue => shortValue,
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            byte[] bytes when int.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var parsedValue) => parsedValue,
            _ => 0
        };
    }
}
