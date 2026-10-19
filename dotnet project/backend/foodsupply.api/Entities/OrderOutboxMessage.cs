namespace FoodSupply.Api.Entities;

/// <summary>
/// Stores an integration event until RabbitMQ confirms that it accepted it.
/// </summary>
public sealed class OrderOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EventType { get; set; } = string.Empty;

    public string RoutingKey { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }
}
