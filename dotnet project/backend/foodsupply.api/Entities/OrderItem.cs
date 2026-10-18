namespace FoodSupply.Api.Entities;

public sealed class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int MenuItemId { get; set; }

    // Snapshots preserve the ordered name and price if the menu changes later.
    public string ItemName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public string? SpecialInstructions { get; set; }

    public Order Order { get; set; } = null!;

    public MenuItem MenuItem { get; set; } = null!;
}
