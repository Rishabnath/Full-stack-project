using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Repositories.Interfaces;

public interface IRestaurantRepository
{
    Task<(IReadOnlyList<Restaurant> Restaurants, int TotalCount)> GetActivePageAsync(
        string? city,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Restaurant?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Restaurant>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
