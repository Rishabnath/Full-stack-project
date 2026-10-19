using FoodSupply.Api.DTOs.MenuItems;

namespace FoodSupply.Api.Services.Interfaces;

public interface IMenuItemService
{
    Task<IReadOnlyList<MenuItemResponse>> GetAvailableForRestaurantAsync(
        int restaurantId,
        CancellationToken cancellationToken = default);

    Task<MenuItemResponse> GetAvailableByIdAsync(
        int restaurantId,
        int menuItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuItemResponse>> GetManageableForRestaurantAsync(
        int restaurantId,
        Guid callerUserId,
        CancellationToken cancellationToken = default);

    Task<MenuItemResponse> CreateAsync(
        int restaurantId,
        Guid callerUserId,
        CreateMenuItemRequest request,
        CancellationToken cancellationToken = default);

    Task<MenuItemResponse> UpdateAsync(
        int restaurantId,
        int menuItemId,
        Guid callerUserId,
        UpdateMenuItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        int restaurantId,
        int menuItemId,
        Guid callerUserId,
        CancellationToken cancellationToken = default);
}
