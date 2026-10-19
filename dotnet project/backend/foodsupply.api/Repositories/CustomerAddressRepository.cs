using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Repositories;

public sealed class CustomerAddressRepository : ICustomerAddressRepository
{
    private readonly FoodSupplyDbContext _dbContext;

    public CustomerAddressRepository(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerAddress>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerAddresses
            .AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<CustomerAddress?> GetByIdAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        // Scope the lookup by owner so another user's address is indistinguishable from a missing ID.
        return _dbContext.CustomerAddresses
            .FirstOrDefaultAsync(
                address => address.UserId == userId && address.Id == addressId,
                cancellationToken);
    }

    public async Task AddAsync(
        CustomerAddress address,
        bool makeDefault,
        CancellationToken cancellationToken = default)
    {
        if (makeDefault)
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            await _dbContext.CustomerAddresses
                .Where(existing => existing.UserId == address.UserId && existing.IsDefault)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(existing => existing.IsDefault, false),
                    cancellationToken);

            address.IsDefault = true;
            await _dbContext.CustomerAddresses.AddAsync(address, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        address.IsDefault = false;
        await _dbContext.CustomerAddresses.AddAsync(address, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetDefaultAsync(
        Guid userId,
        CustomerAddress address,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        await _dbContext.CustomerAddresses
            .Where(existing =>
                existing.UserId == userId &&
                existing.Id != address.Id &&
                existing.IsDefault)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(existing => existing.IsDefault, false),
                cancellationToken);

        address.IsDefault = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid userId,
        CustomerAddress address,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        if (address.IsDefault)
        {
            // Clear the current default inside the transaction before promoting its replacement.
            await _dbContext.CustomerAddresses
                .Where(existing => existing.UserId == userId && existing.Id == address.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(existing => existing.IsDefault, false),
                    cancellationToken);

            var replacementDefault = await _dbContext.CustomerAddresses
                .Where(existing => existing.UserId == userId && existing.Id != address.Id)
                .OrderByDescending(existing => existing.CreatedAtUtc)
                .ThenByDescending(existing => existing.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (replacementDefault is not null)
            {
                replacementDefault.IsDefault = true;
            }
        }

        _dbContext.CustomerAddresses.Remove(address);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
