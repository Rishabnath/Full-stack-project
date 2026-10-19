using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Repositories.Interfaces;

public interface IUserSessionRepository
{
    Task AddAsync(
        UserSession session,
        CancellationToken cancellationToken = default);

    Task<UserSession?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveSessionAsync(
        Guid userId,
        Guid sessionId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> TryRevokeAsync(
        Guid userId,
        Guid sessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> TryRotateAsync(
        Guid currentSessionId,
        string currentRefreshTokenHash,
        DateTime nowUtc,
        UserSession replacementSession,
        CancellationToken cancellationToken = default);

    Task RevokeByRefreshTokenHashAsync(
        string refreshTokenHash,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
