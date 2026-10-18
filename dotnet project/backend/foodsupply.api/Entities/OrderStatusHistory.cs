namespace FoodSupply.Api.Entities;

public sealed class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Note { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public Order Order { get; set; } = null!;

    public ApplicationUser? ChangedByUser { get; set; }
}
