using FoodSupply.Api.Entities;

namespace FoodSupply.Api.DTOs.Orders;

public sealed class OrderSummaryResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public DateTime PlacedAtUtc { get; set; }
}
