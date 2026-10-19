using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Repositories.Interfaces;

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> GetAvailableByRestaurantIdAsync(
        int restaurantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuItem>> GetManageableByRestaurantIdAsync(
        int restaurantId,
        CancellationToken cancellationToken = default);

    Task<MenuItem?> GetByIdAsync(
        int restaurantId,
        int menuItemId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MenuItem menuItem,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
