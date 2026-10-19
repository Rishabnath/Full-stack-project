using FoodSupply.Api.DTOs.MenuItems;
using FoodSupply.Api.Caching;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Services.Interfaces;

namespace FoodSupply.Api.Services;

public sealed class MenuItemService : IMenuItemService
{
    private const decimal MaximumPrice = 9999999999.99m;

    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAppCache _cache;

    public MenuItemService(
        IMenuItemRepository menuItemRepository,
        IRestaurantRepository restaurantRepository,
        ICategoryRepository categoryRepository,
        IUserRepository userRepository,
        IAppCache cache)
    {
        _menuItemRepository = menuItemRepository;
        _restaurantRepository = restaurantRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<MenuItemResponse>> GetAvailableForRestaurantAsync(
        int restaurantId,
        CancellationToken cancellationToken = default)
    {
        ValidateRestaurantId(restaurantId);

        return await _cache.GetOrCreateAsync(
            $"catalog:menu-items:restaurant:{restaurantId}:available",
            CacheDurations.Catalog,
            async token =>
            {
                var restaurant = await _restaurantRepository.GetByIdAsync(
                    restaurantId,
                    token);

                if (restaurant is null || !restaurant.IsActive)
                {
                    throw new NotFoundException(
                        $"Active restaurant with ID {restaurantId} was not found.");
                }

                var menuItems = await _menuItemRepository.GetAvailableByRestaurantIdAsync(
                    restaurantId,
                    token);

                return menuItems.Select(MapToResponse).ToList();
            },
            cancellationToken,
            CacheTags.MenuItems,
            CacheTags.Restaurants);
    }

    public async Task<MenuItemResponse> GetAvailableByIdAsync(
        int restaurantId,
        int menuItemId,
        CancellationToken cancellationToken = default)
    {
        ValidateRestaurantId(restaurantId);
        ValidateMenuItemId(menuItemId);

        return await _cache.GetOrCreateAsync(
            $"catalog:menu-items:restaurant:{restaurantId}:available:{menuItemId}",
            CacheDurations.Catalog,
            async token =>
            {
                var restaurant = await _restaurantRepository.GetByIdAsync(
                    restaurantId,
                    token);

                if (restaurant is null || !restaurant.IsActive)
                {
                    throw new NotFoundException(
                        $"Active restaurant with ID {restaurantId} was not found.");
                }

                var menuItem = await _menuItemRepository.GetByIdAsync(
                    restaurantId,
                    menuItemId,
                    token);

                if (menuItem is null || !menuItem.IsAvailable || !menuItem.Category.IsActive)
                {
                    throw new NotFoundException(
                        $"Available menu item with ID {menuItemId} was not found in restaurant {restaurantId}.");
                }

                return MapToResponse(menuItem);
            },
            cancellationToken,
            CacheTags.MenuItems,
            CacheTags.Restaurants);
    }

    public async Task<IReadOnlyList<MenuItemResponse>> GetManageableForRestaurantAsync(
        int restaurantId,
        Guid callerUserId,
        CancellationToken cancellationToken = default)
    {
        await GetRestaurantForManagerAsync(
            restaurantId,
            callerUserId,
            cancellationToken);

        return await _cache.GetOrCreateAsync(
            $"menu-items:manage:{restaurantId}:{callerUserId:D}",
            CacheDurations.UserData,
            async token =>
            {
                var menuItems = await _menuItemRepository.GetManageableByRestaurantIdAsync(
                    restaurantId,
                    token);

                return menuItems.Select(MapToResponse).ToList();
            },
            cancellationToken,
            CacheTags.MenuItems);
    }

    public async Task<MenuItemResponse> CreateAsync(
        int restaurantId,
        Guid callerUserId,
        CreateMenuItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var restaurant = await GetRestaurantForManagerAsync(
            restaurantId,
            callerUserId,
            cancellationToken);

        if (request is null)
        {
            throw new BadRequestException("Menu item request body is required.");
        }

        var category = await GetActiveCategoryAsync(
            request.CategoryId,
            cancellationToken);

        var menuItem = new MenuItem
        {
            RestaurantId = restaurant.Id,
            CategoryId = category.Id,
            Category = category,
            Name = NormalizeRequiredText(request.Name, "Menu item name", 2, 150),
            Description = NormalizeOptionalText(request.Description, "Description", 1000),
            Price = NormalizePrice(request.Price),
            ImageUrl = NormalizeImageUrl(request.ImageUrl),
            IsAvailable = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _menuItemRepository.AddAsync(menuItem, cancellationToken);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);
        InvalidateMenu(restaurantId);

        return MapToResponse(menuItem);
    }

    public async Task<MenuItemResponse> UpdateAsync(
        int restaurantId,
        int menuItemId,
        Guid callerUserId,
        UpdateMenuItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetRestaurantForManagerAsync(
            restaurantId,
            callerUserId,
            cancellationToken);
        ValidateMenuItemId(menuItemId);

        if (request is null)
        {
            throw new BadRequestException("Menu item request body is required.");
        }

        var menuItem = await _menuItemRepository.GetByIdAsync(
            restaurantId,
            menuItemId,
            cancellationToken);

        if (menuItem is null)
        {
            throw new NotFoundException(
                $"Menu item with ID {menuItemId} was not found in restaurant {restaurantId}.");
        }

        var category = await GetActiveCategoryAsync(
            request.CategoryId,
            cancellationToken);

        menuItem.CategoryId = category.Id;
        menuItem.Category = category;
        menuItem.Name = NormalizeRequiredText(request.Name, "Menu item name", 2, 150);
        menuItem.Description = NormalizeOptionalText(
            request.Description,
            "Description",
            1000);
        menuItem.Price = NormalizePrice(request.Price);
        menuItem.ImageUrl = NormalizeImageUrl(request.ImageUrl);
        menuItem.IsAvailable = request.IsAvailable
            ?? throw new BadRequestException("Availability must be specified.");

        await _menuItemRepository.SaveChangesAsync(cancellationToken);
        InvalidateMenu(restaurantId);

        return MapToResponse(menuItem);
    }

    public async Task DeactivateAsync(
        int restaurantId,
        int menuItemId,
        Guid callerUserId,
        CancellationToken cancellationToken = default)
    {
        await GetRestaurantForManagerAsync(
            restaurantId,
            callerUserId,
            cancellationToken);
        ValidateMenuItemId(menuItemId);

        var menuItem = await _menuItemRepository.GetByIdAsync(
            restaurantId,
            menuItemId,
            cancellationToken);

        if (menuItem is null)
        {
            throw new NotFoundException(
                $"Menu item with ID {menuItemId} was not found in restaurant {restaurantId}.");
        }

        if (!menuItem.IsAvailable)
        {
            return;
        }

        // Keep the row because past orders may refer to it. It disappears from
        // customer menus, but the order history remains valid.
        menuItem.IsAvailable = false;
        await _menuItemRepository.SaveChangesAsync(cancellationToken);
        InvalidateMenu(restaurantId);
    }

    private async Task<Restaurant> GetRestaurantForManagerAsync(
        int restaurantId,
        Guid callerUserId,
        CancellationToken cancellationToken)
    {
        ValidateRestaurantId(restaurantId);
        ValidateUserId(callerUserId);

        var restaurant = await _restaurantRepository.GetByIdAsync(
            restaurantId,
            cancellationToken);

        if (restaurant is null)
        {
            throw new NotFoundException($"Restaurant with ID {restaurantId} was not found.");
        }

        var caller = await _userRepository.GetByIdAsync(
            callerUserId,
            cancellationToken);

        if (caller is null || !caller.IsActive)
        {
            throw new UnauthorizedException("Your account is unavailable.");
        }

        if (caller.Role == UserRole.Admin)
        {
            return restaurant;
        }

        if (caller.Role != UserRole.RestaurantOwner || restaurant.OwnerId != caller.Id)
        {
            throw new ForbiddenException(
                "Only this restaurant's owner or an administrator can manage its menu.");
        }

        return restaurant;
    }

    private async Task<Category> GetActiveCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId <= 0)
        {
            throw new BadRequestException("A valid category ID is required.");
        }

        var category = await _categoryRepository.GetByIdAsync(
            categoryId,
            cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category with ID {categoryId} was not found.");
        }

        if (!category.IsActive)
        {
            throw new ConflictException("Menu items cannot use an inactive category.");
        }

        return category;
    }

    private static void ValidateRestaurantId(int restaurantId)
    {
        if (restaurantId <= 0)
        {
            throw new BadRequestException("Restaurant ID must be greater than zero.");
        }
    }

    private static void ValidateMenuItemId(int menuItemId)
    {
        if (menuItemId <= 0)
        {
            throw new BadRequestException("Menu item ID must be greater than zero.");
        }
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("A valid authenticated user is required.");
        }
    }

    private static string NormalizeRequiredText(
        string? value,
        string fieldName,
        int minimumLength,
        int maximumLength)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new BadRequestException($"{fieldName} is required.");
        }

        if (normalizedValue.Length < minimumLength ||
            normalizedValue.Length > maximumLength)
        {
            throw new BadRequestException(
                $"{fieldName} must be between {minimumLength} and {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(
        string? value,
        string fieldName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new BadRequestException(
                $"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static decimal NormalizePrice(decimal price)
    {
        if (price <= 0 || price > MaximumPrice || decimal.Round(price, 2) != price)
        {
            throw new BadRequestException(
                "Price must be greater than zero and have no more than two decimal places.");
        }

        return price;
    }

    private static string? NormalizeImageUrl(string? imageUrl)
    {
        var normalizedUrl = NormalizeOptionalText(imageUrl, "Image URL", 2048);
        if (normalizedUrl is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new BadRequestException(
                "Image URL must be an absolute HTTP or HTTPS URL.");
        }

        return normalizedUrl;
    }

    private static MenuItemResponse MapToResponse(MenuItem menuItem)
    {
        return new MenuItemResponse
        {
            Id = menuItem.Id,
            RestaurantId = menuItem.RestaurantId,
            CategoryId = menuItem.CategoryId,
            CategoryName = menuItem.Category.Name,
            Name = menuItem.Name,
            Description = menuItem.Description,
            Price = menuItem.Price,
            ImageUrl = menuItem.ImageUrl,
            IsAvailable = menuItem.IsAvailable,
            CreatedAtUtc = menuItem.CreatedAtUtc
        };
    }

    private void InvalidateMenu(int restaurantId)
    {
        _cache.EvictTag(CacheTags.MenuItems);
    }
}
