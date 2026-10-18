namespace FoodSupply.Api.Entities;

public sealed class Restaurant
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? ImageUrl { get; set; }

    public Guid? OwnerId { get; set; }

    public ApplicationUser? Owner { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
