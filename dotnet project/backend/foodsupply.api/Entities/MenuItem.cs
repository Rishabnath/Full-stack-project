namespace FoodSupply.Api.Entities;

public sealed class MenuItem
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Restaurant Restaurant { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
