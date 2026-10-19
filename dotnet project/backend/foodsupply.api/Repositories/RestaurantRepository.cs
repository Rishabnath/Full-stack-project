using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Repositories;

public sealed class RestaurantRepository : IRestaurantRepository
{
    private readonly FoodSupplyDbContext _dbContext;

    public RestaurantRepository(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<Restaurant> Restaurants, int TotalCount)> GetActivePageAsync(
        string? city,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Restaurants
            .AsNoTracking()
            .Where(restaurant => restaurant.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim().ToLowerInvariant();
            query = query.Where(
                restaurant => restaurant.City.ToLower() == normalizedCity);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var restaurants = await query
            .OrderBy(restaurant => restaurant.Name)
            .ThenBy(restaurant => restaurant.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (restaurants, totalCount);
    }

    public Task<Restaurant?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Restaurants
            .FirstOrDefaultAsync(restaurant => restaurant.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Restaurant>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Restaurants
            .AsNoTracking()
            .Where(restaurant => restaurant.OwnerId == ownerId)
            .OrderByDescending(restaurant => restaurant.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Restaurants.AddAsync(restaurant, cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
