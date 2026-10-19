using FoodSupply.Api.DTOs.Restaurants;
using FoodSupply.Api.Caching;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Services.Interfaces;

namespace FoodSupply.Api.Services;

public sealed class RestaurantService : IRestaurantService
{
    private const int MaximumPageSize = 100;

    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAppCache _cache;

    public RestaurantService(
        IRestaurantRepository restaurantRepository,
        IUserRepository userRepository,
        IAppCache cache)
    {
        _restaurantRepository = restaurantRepository;
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<RestaurantListResponse> GetActivePageAsync(
        int page,
        int pageSize,
        string? city,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new BadRequestException("Page must be at least 1.");
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new BadRequestException(
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        var skipLong = (long)(page - 1) * pageSize;
        if (skipLong > int.MaxValue)
        {
            throw new BadRequestException("The requested page is too large.");
        }

        var normalizedCity = NormalizeOptionalText(city, "City", 100);
        var cityKey = normalizedCity?.ToUpperInvariant() ?? "all";
        var cacheKey = $"catalog:restaurants:page:{cityKey}:{page}:{pageSize}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            CacheDurations.Catalog,
            async token =>
            {
                var (restaurants, totalCount) = await _restaurantRepository.GetActivePageAsync(
                    normalizedCity,
                    (int)skipLong,
                    pageSize,
                    token);

                return new RestaurantListResponse
                {
                    Items = restaurants.Select(MapToResponse).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            },
            cancellationToken,
            CacheTags.Restaurants);
    }

    public async Task<RestaurantResponse> GetActiveByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidateRestaurantId(id);

        return await _cache.GetOrCreateAsync(
            $"catalog:restaurants:active:{id}",
            CacheDurations.Catalog,
            async token =>
            {
                var restaurant = await _restaurantRepository.GetByIdAsync(id, token);

                if (restaurant is null || !restaurant.IsActive)
                {
                    throw new NotFoundException(
                        $"Active restaurant with ID {id} was not found.");
                }

                return MapToResponse(restaurant);
            },
            cancellationToken,
            CacheTags.Restaurants);
    }

    public async Task<IReadOnlyList<RestaurantResponse>> GetMineAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(ownerId);

        return await _cache.GetOrCreateAsync(
            $"restaurants:owner:{ownerId:D}",
            CacheDurations.UserData,
            async token =>
            {
                var restaurants = await _restaurantRepository.GetByOwnerIdAsync(
                    ownerId,
                    token);

                return restaurants.Select(MapToResponse).ToList();
            },
            cancellationToken,
            CacheTags.Restaurants);
    }

    public async Task<RestaurantResponse> CreateAsync(
        Guid ownerId,
        CreateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(ownerId);

        if (request is null)
        {
            throw new BadRequestException("Restaurant request body is required.");
        }

        var owner = await _userRepository.GetByIdAsync(ownerId, cancellationToken);
        if (owner is null || !owner.IsActive)
        {
            throw new UnauthorizedException("Your account is unavailable.");
        }

        if (owner.Role != UserRole.RestaurantOwner)
        {
            throw new ForbiddenException(
                "An approved RestaurantOwner account is required to create a restaurant.");
        }

        var restaurant = new Restaurant
        {
            Name = NormalizeRequiredText(request.Name, "Restaurant name", 2, 150),
            Description = NormalizeOptionalText(request.Description, "Description", 500),
            Address = NormalizeRequiredText(request.Address, "Address", 5, 250),
            City = NormalizeRequiredText(request.City, "City", 2, 100),
            PhoneNumber = NormalizeOptionalText(request.PhoneNumber, "Phone number", 30),
            ImageUrl = NormalizeImageUrl(request.ImageUrl),
            OwnerId = owner.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _restaurantRepository.AddAsync(restaurant, cancellationToken);
        await _restaurantRepository.SaveChangesAsync(cancellationToken);
        InvalidateRestaurantCatalog(ownerId);

        return MapToResponse(restaurant);
    }

    public async Task<RestaurantResponse> UpdateAsync(
        int id,
        Guid callerUserId,
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRestaurantId(id);
        ValidateUserId(callerUserId);

        if (request is null)
        {
            throw new BadRequestException("Restaurant request body is required.");
        }

        var restaurant = await _restaurantRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (restaurant is null)
        {
            throw new NotFoundException($"Restaurant with ID {id} was not found.");
        }

        var caller = await _userRepository.GetByIdAsync(
            callerUserId,
            cancellationToken);

        if (caller is null || !caller.IsActive)
        {
            throw new UnauthorizedException("Your account is unavailable.");
        }

        var isAdministrator = caller.Role == UserRole.Admin;
        var isOwner = caller.Role == UserRole.RestaurantOwner;

        if (!isAdministrator && !isOwner)
        {
            throw new ForbiddenException(
                "Only the restaurant owner or an administrator can update this restaurant.");
        }

        if (!isAdministrator && restaurant.OwnerId != caller.Id)
        {
            throw new ForbiddenException(
                "You can update only restaurants that belong to your account.");
        }

        restaurant.Name = NormalizeRequiredText(request.Name, "Restaurant name", 2, 150);
        restaurant.Description = NormalizeOptionalText(
            request.Description,
            "Description",
            500);
        restaurant.Address = NormalizeRequiredText(request.Address, "Address", 5, 250);
        restaurant.City = NormalizeRequiredText(request.City, "City", 2, 100);
        restaurant.PhoneNumber = NormalizeOptionalText(
            request.PhoneNumber,
            "Phone number",
            30);
        restaurant.ImageUrl = NormalizeImageUrl(request.ImageUrl);

        await _restaurantRepository.SaveChangesAsync(cancellationToken);
        InvalidateRestaurantCatalog(restaurant.OwnerId);

        return MapToResponse(restaurant);
    }

    public async Task DeactivateAsync(
        int id,
        Guid callerUserId,
        CancellationToken cancellationToken = default)
    {
        var restaurant = await GetRestaurantForManagerAsync(
            id,
            callerUserId,
            cancellationToken);

        if (!restaurant.IsActive)
        {
            return;
        }

        restaurant.IsActive = false;
        await _restaurantRepository.SaveChangesAsync(cancellationToken);
        InvalidateRestaurantCatalog(restaurant.OwnerId);
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

        var caller = await _userRepository.GetByIdAsync(callerUserId, cancellationToken);
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
                "Only this restaurant's owner or an administrator can manage it.");
        }

        return restaurant;
    }

    private static void ValidateRestaurantId(int id)
    {
        if (id <= 0)
        {
            throw new BadRequestException(
                "Restaurant ID must be greater than zero.");
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

    private static RestaurantResponse MapToResponse(Restaurant restaurant)
    {
        return new RestaurantResponse
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            Description = restaurant.Description,
            Address = restaurant.Address,
            City = restaurant.City,
            PhoneNumber = restaurant.PhoneNumber,
            ImageUrl = restaurant.ImageUrl,
            IsActive = restaurant.IsActive,
            CreatedAtUtc = restaurant.CreatedAtUtc
        };
    }

    private void InvalidateRestaurantCatalog(Guid? ownerId)
    {
        _cache.EvictTag(CacheTags.Restaurants);
        _cache.EvictTag(CacheTags.MenuItems);
    }
}
