namespace FoodSupply.Api.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    // The default guest account works only for a local RabbitMQ connection.
    // Override credentials with .NET User Secrets or deployment secrets elsewhere.
    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string ExchangeName { get; set; } = "foodsupply.events";

    public string QueueName { get; set; } = "foodsupply.order-events";

    public string RoutingKey { get; set; } = "orders.#";

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int MessageLockSeconds { get; set; } = 60;

    public int MaximumRetryDelaySeconds { get; set; } = 300;
}
