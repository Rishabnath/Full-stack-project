using FoodSupply.Api.DTOs.Categories;
using FoodSupply.Api.Caching;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Services.Interfaces;

namespace FoodSupply.Api.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAppCache _cache;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IAppCache cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            "catalog:categories:active:all",
            CacheDurations.Catalog,
            async token =>
            {
                var categories = await _categoryRepository.GetAllAsync(token);
                return categories
                    .Where(category => category.IsActive)
                    .Select(MapToResponse)
                    .ToList();
            },
            cancellationToken,
            CacheTags.Categories);
    }

    public async Task<CategoryResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new BadRequestException("Category ID must be greater than zero.");
        }

        return await _cache.GetOrCreateAsync(
            $"catalog:categories:active:{id}",
            CacheDurations.Catalog,
            async token =>
            {
                var category = await _categoryRepository.GetByIdAsync(id, token);

                if (category is null || !category.IsActive)
                {
                    throw new NotFoundException(
                        $"Active category with ID {id} was not found.");
                }

                return MapToResponse(category);
            },
            cancellationToken,
            CacheTags.Categories);
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new BadRequestException("Category request body is required.");
        }

        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);

        if (await _categoryRepository.ExistsByNameAsync(
                name,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = description,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);
        InvalidateCatalog();

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new BadRequestException("Category ID must be greater than zero.");
        }

        if (request is null)
        {
            throw new BadRequestException("Category request body is required.");
        }

        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category with ID {id} was not found.");
        }

        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);

        if (await _categoryRepository.ExistsByNameAsync(
                name,
                excludingCategoryId: id,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        category.Name = name;
        category.Description = description;

        await _categoryRepository.SaveChangesAsync(cancellationToken);
        InvalidateCatalog();

        return MapToResponse(category);
    }

    public async Task DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new BadRequestException("Category ID must be greater than zero.");
        }

        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category with ID {id} was not found.");
        }

        if (!category.IsActive)
        {
            return;
        }

        category.IsActive = false;
        await _categoryRepository.SaveChangesAsync(cancellationToken);
        InvalidateCatalog();
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Category name is required.");
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length < 2 || normalizedName.Length > 100)
        {
            throw new BadRequestException(
                "Category name must be between 2 and 100 characters.");
        }

        return normalizedName;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalizedDescription = description.Trim();

        if (normalizedDescription.Length > 300)
        {
            throw new BadRequestException(
                "Category description cannot exceed 300 characters.");
        }

        return normalizedDescription;
    }

    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        };
    }

    private void InvalidateCatalog()
    {
        _cache.EvictTag(CacheTags.Categories);
        _cache.EvictTag(CacheTags.MenuItems);
    }
}
