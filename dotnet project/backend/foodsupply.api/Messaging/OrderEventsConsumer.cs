using System.Text.Json;
using FoodSupply.Api.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FoodSupply.Api.Messaging;

/// <summary>
/// Receives order lifecycle events from RabbitMQ and records their processing
/// through the application's structured logs. A later consumer can replace or
/// extend this behavior with notifications, analytics, or other side effects.
/// </summary>
public sealed class OrderEventsConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OrderEventsConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderEventsConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<OrderEventsConsumer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await StartConsumerAsync(stoppingToken);

                    // The RabbitMQ client restores the consumer after network recovery.
                    await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Could not start the RabbitMQ order event consumer. Retrying.");

                    await ResetRabbitMqAsync();
                    await DelayBeforeRetryAsync(stoppingToken);
                }
            }
        }
        finally
        {
            await ResetRabbitMqAsync();
        }
    }

    private async Task StartConsumerAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "FoodSupply.Api order event consumer"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, delivery) =>
            await HandleDeliveryAsync(delivery, cancellationToken);

        await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Listening for order lifecycle events on RabbitMQ queue {QueueName}.",
            _options.QueueName);
    }

    private async Task HandleDeliveryAsync(
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        try
        {
            LogOrderEvent(delivery);

            await _channel!.BasicAckAsync(
                deliveryTag: delivery.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Leave the event unacknowledged; RabbitMQ will redeliver it after shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not process RabbitMQ order event {MessageId}; it will be requeued.",
                delivery.BasicProperties.MessageId);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            if (_channel?.IsOpen == true)
            {
                await _channel.BasicNackAsync(
                    deliveryTag: delivery.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private void LogOrderEvent(BasicDeliverEventArgs delivery)
    {
        var payload = delivery.Body.ToArray();
        var eventType = delivery.BasicProperties.Type;

        switch (eventType)
        {
            case nameof(OrderPlacedEvent):
            {
                var message = JsonSerializer.Deserialize<OrderPlacedEvent>(payload)
                    ?? throw new JsonException("OrderPlacedEvent payload was empty.");

                _logger.LogInformation(
                    "Consumed {EventType} for order {OrderId} ({OrderNumber}) at restaurant {RestaurantId}; total {TotalAmount} {CurrencyCode}.",
                    eventType,
                    message.OrderId,
                    message.OrderNumber,
                    message.RestaurantId,
                    message.TotalAmount,
                    message.CurrencyCode);
                break;
            }

            case nameof(OrderStatusChangedEvent):
            {
                var message = JsonSerializer.Deserialize<OrderStatusChangedEvent>(payload)
                    ?? throw new JsonException("OrderStatusChangedEvent payload was empty.");

                _logger.LogInformation(
                    "Consumed {EventType} for order {OrderId} ({OrderNumber}): {PreviousStatus} -> {CurrentStatus}; changed by {ChangedByUserId}.",
                    eventType,
                    message.OrderId,
                    message.OrderNumber,
                    message.PreviousStatus,
                    message.CurrentStatus,
                    message.ChangedByUserId);
                break;
            }

            case nameof(OrderDeliveryClaimedEvent):
            {
                var message = JsonSerializer.Deserialize<OrderDeliveryClaimedEvent>(payload)
                    ?? throw new JsonException("OrderDeliveryClaimedEvent payload was empty.");

                _logger.LogInformation(
                    "Consumed {EventType} for order {OrderId} ({OrderNumber}); assigned to delivery partner {DeliveryPartnerId}.",
                    eventType,
                    message.OrderId,
                    message.OrderNumber,
                    message.DeliveryPartnerId);
                break;
            }

            default:
                // This API only understands its three current event contracts.
                // Acknowledge unknown types so a future producer cannot block the queue.
                _logger.LogWarning(
                    "Acknowledging unsupported order event type {EventType} with message ID {MessageId}.",
                    eventType,
                    delivery.BasicProperties.MessageId);
                break;
        }
    }

    private async Task ResetRabbitMqAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async Task DelayBeforeRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Application shutdown interrupts the retry delay.
        }
    }
}
