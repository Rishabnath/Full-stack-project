using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Repositories.Interfaces;

public interface IOrderOutboxRepository
{
    Task<IReadOnlyList<OrderOutboxMessage>> ClaimBatchAsync(
        int batchSize,
        DateTime nowUtc,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(
        Guid messageId,
        DateTime publishedAtUtc,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid messageId,
        string error,
        DateTime nextAttemptAtUtc,
        CancellationToken cancellationToken = default);
}
