using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Repositories;

public sealed class OrderOutboxRepository : IOrderOutboxRepository
{
    private const int MaximumStoredErrorLength = 2000;

    private readonly FoodSupplyDbContext _dbContext;

    public OrderOutboxRepository(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OrderOutboxMessage>> ClaimBatchAsync(
        int batchSize,
        DateTime nowUtc,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        // SKIP LOCKED lets multiple API instances claim different rows safely.
        var messages = await _dbContext.OrderOutboxMessages
            .FromSqlInterpolated($"""
                SELECT *
                FROM "outbox_messages"
                WHERE "published_at_utc" IS NULL
                  AND "next_attempt_at_utc" <= {nowUtc}
                  AND ("locked_until_utc" IS NULL OR "locked_until_utc" <= {nowUtc})
                ORDER BY "occurred_at_utc", "id"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var lockedUntilUtc = nowUtc.Add(lockDuration);
        foreach (var message in messages)
        {
            message.LockedUntilUtc = lockedUntilUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages;
    }

    public async Task MarkPublishedAsync(
        Guid messageId,
        DateTime publishedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderOutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.PublishedAtUtc, publishedAtUtc)
                    .SetProperty(message => message.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(message => message.LastError, (string?)null),
                cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        DateTime nextAttemptAtUtc,
        CancellationToken cancellationToken = default)
    {
        var storedError = error.Length <= MaximumStoredErrorLength
            ? error
            : error[..MaximumStoredErrorLength];

        await _dbContext.OrderOutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                    .SetProperty(message => message.NextAttemptAtUtc, nextAttemptAtUtc)
                    .SetProperty(message => message.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(message => message.LastError, storedError),
                cancellationToken);
    }
}
