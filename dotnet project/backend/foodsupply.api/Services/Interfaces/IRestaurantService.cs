using FoodSupply.Api.DTOs.Restaurants;

namespace FoodSupply.Api.Services.Interfaces;

public interface IRestaurantService
{
    Task<RestaurantListResponse> GetActivePageAsync(
        int page,
        int pageSize,
        string? city,
        CancellationToken cancellationToken = default);

    Task<RestaurantResponse> GetActiveByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RestaurantResponse>> GetMineAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<RestaurantResponse> CreateAsync(
        Guid ownerId,
        CreateRestaurantRequest request,
        CancellationToken cancellationToken = default);

    Task<RestaurantResponse> UpdateAsync(
        int id,
        Guid callerUserId,
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        int id,
        Guid callerUserId,
        CancellationToken cancellationToken = default);
}
