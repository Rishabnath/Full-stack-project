using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Repositories;

public sealed class MenuItemRepository : IMenuItemRepository
{
    private readonly FoodSupplyDbContext _dbContext;

    public MenuItemRepository(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MenuItem>> GetAvailableByRestaurantIdAsync(
        int restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(menuItem => menuItem.Category)
            .Where(menuItem =>
                menuItem.RestaurantId == restaurantId &&
                menuItem.IsAvailable &&
                menuItem.Category.IsActive)
            .OrderBy(menuItem => menuItem.Category.Name)
            .ThenBy(menuItem => menuItem.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItem>> GetManageableByRestaurantIdAsync(
        int restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(menuItem => menuItem.Category)
            .Where(menuItem => menuItem.RestaurantId == restaurantId)
            .OrderBy(menuItem => menuItem.Category.Name)
            .ThenBy(menuItem => menuItem.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<MenuItem?> GetByIdAsync(
        int restaurantId,
        int menuItemId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItems
            .Include(menuItem => menuItem.Category)
            .FirstOrDefaultAsync(
                menuItem =>
                    menuItem.RestaurantId == restaurantId &&
                    menuItem.Id == menuItemId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItem>> GetAvailableByIdsAsync(
        int restaurantId,
        IReadOnlyCollection<int> menuItemIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(menuItem => menuItem.Category)
            .Where(menuItem =>
                menuItem.RestaurantId == restaurantId &&
                menuItemIds.Contains(menuItem.Id) &&
                menuItem.IsAvailable &&
                menuItem.Category.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        MenuItem menuItem,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.MenuItems.AddAsync(menuItem, cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
