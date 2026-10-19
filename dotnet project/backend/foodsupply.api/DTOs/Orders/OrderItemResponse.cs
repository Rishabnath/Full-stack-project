namespace FoodSupply.Api.DTOs.Orders;

public sealed class OrderItemResponse
{
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? SpecialInstructions { get; set; }
}
