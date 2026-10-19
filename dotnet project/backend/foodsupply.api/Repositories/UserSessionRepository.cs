using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Repositories;

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly FoodSupplyDbContext _dbContext;

    public UserSessionRepository(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        UserSession session,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.UserSessions.AddAsync(session, cancellationToken);
    }

    public Task<UserSession?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSessions
            .AsNoTracking()
            .Include(session => session.User)
            .FirstOrDefaultAsync(
                session => session.RefreshTokenHash == refreshTokenHash,
                cancellationToken);
    }

    public async Task<bool> TryRotateAsync(
        Guid currentSessionId,
        string currentRefreshTokenHash,
        DateTime nowUtc,
        UserSession replacementSession,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        // This conditional update makes rotation single-use: only one concurrent
        // request can revoke the old token and create its replacement.
        var updatedRows = await _dbContext.UserSessions
            .Where(session =>
                session.Id == currentSessionId &&
                session.RefreshTokenHash == currentRefreshTokenHash &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.RevokedAtUtc, nowUtc)
                    .SetProperty(
                        session => session.ReplacedBySessionId,
                        replacementSession.Id),
                cancellationToken);

        if (updatedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _dbContext.UserSessions.AddAsync(
            replacementSession,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task RevokeByRefreshTokenHashAsync(
        string refreshTokenHash,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.UserSessions
            .Where(session =>
                session.RefreshTokenHash == refreshTokenHash &&
                session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAtUtc,
                    revokedAtUtc),
                cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
