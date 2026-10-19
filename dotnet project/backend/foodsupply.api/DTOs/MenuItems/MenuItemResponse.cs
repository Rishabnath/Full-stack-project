namespace FoodSupply.Api.DTOs.MenuItems;

public sealed class MenuItemResponse
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
