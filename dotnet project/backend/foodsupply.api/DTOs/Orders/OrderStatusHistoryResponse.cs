using FoodSupply.Api.Entities;

namespace FoodSupply.Api.DTOs.Orders;

public sealed class OrderStatusHistoryResponse
{
    public OrderStatus Status { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string? Note { get; set; }
}
