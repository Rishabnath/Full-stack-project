using System.ComponentModel.DataAnnotations;
using FoodSupply.Api.Entities;

namespace FoodSupply.Api.DTOs.Orders;

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
