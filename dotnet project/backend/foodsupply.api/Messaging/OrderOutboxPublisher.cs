using System.Text;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FoodSupply.Api.Messaging;

public sealed class OrderOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OrderOutboxPublisher> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderOutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<OrderOutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
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
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var repository = scope.ServiceProvider
                        .GetRequiredService<IOrderOutboxRepository>();

                    var messages = await repository.ClaimBatchAsync(
                        _options.BatchSize,
                        DateTime.UtcNow,
                        TimeSpan.FromSeconds(_options.MessageLockSeconds),
                        stoppingToken);

                    if (messages.Count == 0)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                            stoppingToken);
                        continue;
                    }

                    foreach (var message in messages)
                    {
                        if (stoppingToken.IsCancellationRequested)
                        {
                            break;
                        }

                        await PublishOneAsync(repository, message, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "The order outbox worker could not process a batch. It will retry.");

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

    private async Task PublishOneAsync(
        IOrderOutboxRepository repository,
        OrderOutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureRabbitMqAsync(cancellationToken);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.Id.ToString(),
                Type = message.EventType,
                Timestamp = new AmqpTimestamp(
                    new DateTimeOffset(message.OccurredAtUtc).ToUnixTimeSeconds())
            };

            var body = Encoding.UTF8.GetBytes(message.Payload);

            // With publisher confirmations enabled, this waits for RabbitMQ's confirmation.
            await _channel!.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: message.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            await repository.MarkPublishedAsync(
                message.Id,
                DateTime.UtcNow,
                cancellationToken);

            _logger.LogInformation(
                "Published order event {EventType} for outbox message {MessageId}.",
                message.EventType,
                message.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var nextAttemptCount = message.AttemptCount + 1;
            var exponentialDelaySeconds = Math.Pow(2, Math.Min(nextAttemptCount, 10));
            var retryDelaySeconds = Math.Min(
                exponentialDelaySeconds,
                _options.MaximumRetryDelaySeconds);
            var nextAttemptAtUtc = DateTime.UtcNow.AddSeconds(retryDelaySeconds);

            try
            {
                await repository.MarkFailedAsync(
                    message.Id,
                    exception.Message,
                    nextAttemptAtUtc,
                    cancellationToken);
            }
            finally
            {
                await ResetRabbitMqAsync();
            }

            _logger.LogWarning(
                exception,
                "Could not publish order event {EventType} for outbox message {MessageId}. " +
                "Retry scheduled for {NextAttemptAtUtc}.",
                message.EventType,
                message.Id,
                nextAttemptAtUtc);
        }
    }

    private async Task EnsureRabbitMqAsync(CancellationToken cancellationToken)
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
        {
            return;
        }

        await ResetRabbitMqAsync();

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "FoodSupply.Api order outbox publisher"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);

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

        _logger.LogInformation(
            "Connected to RabbitMQ and declared exchange {ExchangeName} and queue {QueueName}.",
            _options.ExchangeName,
            _options.QueueName);
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
