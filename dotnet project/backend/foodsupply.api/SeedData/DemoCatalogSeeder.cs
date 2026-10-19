using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.SeedData;

/// <summary>
/// Adds a small, repeatable catalog for local development and demonstrations.
/// It leaves a catalog alone once any restaurant has been created.
/// </summary>
public sealed class DemoCatalogSeeder
{
    private readonly FoodSupplyDbContext _dbContext;

    public DemoCatalogSeeder(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.Categories
            .ToListAsync(cancellationToken);

        var categoriesByName = new Dictionary<string, Category>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            categoriesByName.TryAdd(category.Name, category);
        }

        foreach (var categorySeed in CategorySeeds)
        {
            if (categoriesByName.ContainsKey(categorySeed.Name))
            {
                continue;
            }

            var category = new Category
            {
                Name = categorySeed.Name,
                Description = categorySeed.Description,
                IsActive = true
            };

            _dbContext.Categories.Add(category);
            categoriesByName.Add(category.Name, category);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Do not add demo restaurants over real restaurants created by a user.
        if (await _dbContext.Restaurants.AnyAsync(cancellationToken))
        {
            return;
        }

        var restaurants = RestaurantSeeds.Select(seed => new Restaurant
        {
            Name = seed.Name,
            Description = seed.Description,
            Address = seed.Address,
            City = seed.City,
            PhoneNumber = seed.PhoneNumber,
            ImageUrl = seed.ImageUrl,
            IsActive = true,
            MenuItems = seed.MenuItems.Select(item => new MenuItem
            {
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                ImageUrl = item.ImageUrl,
                IsAvailable = true,
                Category = categoriesByName[item.CategoryName]
            }).ToList()
        }).ToList();

        await _dbContext.Restaurants.AddRangeAsync(restaurants, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly CategorySeed[] CategorySeeds =
    [
        new("Biryani", "Aromatic rice dishes cooked with spices and vegetables or meat."),
        new("North Indian", "Curries, breads, grilled dishes, and North Indian favourites."),
        new("South Indian", "Dosas, idlis, vadas, and South Indian comfort food."),
        new("Pizza", "Oven-baked pizzas with classic and Indian-inspired toppings."),
        new("Burgers", "Burgers, fries, and casual comfort food."),
        new("Desserts", "Sweet treats to finish a meal.")
    ];

    private static readonly RestaurantSeed[] RestaurantSeeds =
    [
        new(
            "Hyderabad Spice House",
            "Slow-cooked biryani and rich North Indian curries, made with freshly ground spices.",
            "12 Road No. 2, Banjara Hills",
            "Hyderabad",
            "+91-90000-10001",
            "https://images.unsplash.com/photo-1751200503125-d8cb239f95ba?auto=format&fit=crop&w=1200&q=80",
            [
                new("Hyderabadi Chicken Dum Biryani", "Fragrant basmati rice layered with spiced chicken and herbs.", 289.00m, "Biryani", "https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?auto=format&fit=crop&w=1200&q=80"),
                new("Paneer Butter Masala", "Soft paneer in a creamy tomato and cashew gravy.", 249.00m, "North Indian", "https://images.unsplash.com/photo-1742599361498-79824d24e355?auto=format&fit=crop&w=1200&q=80"),
                new("Chicken Tikka with Naan", "Char-grilled chicken tikka served with buttery naan.", 319.00m, "North Indian", "https://images.unsplash.com/photo-1772730064970-a7b2735c93b9?auto=format&fit=crop&w=1200&q=80")
            ]),
        new(
            "Amma's South Kitchen",
            "A neighbourhood kitchen serving crisp dosas, chutneys, and traditional breakfast favourites.",
            "44 12th Main Road, Indiranagar",
            "Bengaluru",
            "+91-90000-10002",
            "https://images.unsplash.com/photo-1743517894265-c86ab035adef?auto=format&fit=crop&w=1200&q=80",
            [
                new("Mysore Masala Dosa", "Crisp dosa with spiced potato masala, coconut chutney, and sambar.", 169.00m, "South Indian", "https://images.unsplash.com/photo-1743517894265-c86ab035adef?auto=format&fit=crop&w=1200&q=80"),
                new("Idli Sambar", "Steamed rice cakes with lentil sambar and house chutneys.", 119.00m, "South Indian", "https://images.unsplash.com/photo-1743517894265-c86ab035adef?auto=format&fit=crop&w=1200&q=80"),
                new("Medu Vada Plate", "Golden lentil fritters served with sambar and coconut chutney.", 129.00m, "South Indian", "https://images.unsplash.com/photo-1743517894265-c86ab035adef?auto=format&fit=crop&w=1200&q=80")
            ]),
        new(
            "Firestone Pizza Co.",
            "Hand-stretched dough, baked hot, with generous toppings and a crisp crust.",
            "8 Jubilee Hills Road, Jubilee Hills",
            "Hyderabad",
            "+91-90000-10003",
            "https://images.unsplash.com/photo-1659367736714-e3f0344a6266?auto=format&fit=crop&w=1200&q=80",
            [
                new("Farmhouse Paneer Pizza", "Paneer, capsicum, onion, sweet corn, and mozzarella.", 329.00m, "Pizza", "https://images.unsplash.com/photo-1665033628673-7de125eb6b12?auto=format&fit=crop&w=1200&q=80"),
                new("Classic Margherita", "Tomato sauce, mozzarella, and fresh basil on a crisp crust.", 269.00m, "Pizza", "https://images.unsplash.com/photo-1659367736714-e3f0344a6266?auto=format&fit=crop&w=1200&q=80"),
                new("Garlic Bread", "Toasted bread with garlic butter, herbs, and cheese.", 149.00m, "Pizza", "https://images.unsplash.com/photo-1665033628673-7de125eb6b12?auto=format&fit=crop&w=1200&q=80")
            ]),
        new(
            "The Burger Yard",
            "Big, juicy burgers with fresh toppings, house sauces, and crispy sides.",
            "6 Madhapur Main Road, Madhapur",
            "Hyderabad",
            "+91-90000-10004",
            "https://images.unsplash.com/photo-1610440042657-612c34d95e9f?auto=format&fit=crop&w=1200&q=80",
            [
                new("Classic Cheese Burger", "Grilled patty, cheddar, lettuce, tomato, and house sauce.", 229.00m, "Burgers", "https://images.unsplash.com/photo-1610440042657-612c34d95e9f?auto=format&fit=crop&w=1200&q=80"),
                new("Double Smash Burger", "Two seared patties with cheese, pickles, and smoky sauce.", 319.00m, "Burgers", "https://images.unsplash.com/photo-1610440042657-612c34d95e9f?auto=format&fit=crop&w=1200&q=80"),
                new("Chocolate Brownie", "Warm chocolate brownie, baked in-house.", 139.00m, "Desserts", "https://images.unsplash.com/photo-1742599361498-79824d24e355?auto=format&fit=crop&w=1200&q=80")
            ])
    ];

    private sealed record CategorySeed(string Name, string Description);

    private sealed record RestaurantSeed(
        string Name,
        string Description,
        string Address,
        string City,
        string PhoneNumber,
        string ImageUrl,
        IReadOnlyList<MenuItemSeed> MenuItems);

    private sealed record MenuItemSeed(
        string Name,
        string Description,
        decimal Price,
        string CategoryName,
        string ImageUrl);
}
