using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.Orders;

public sealed class CreateOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int MenuItemId { get; set; }

    [Range(1, 50)]
    public int Quantity { get; set; }

    [StringLength(500)]
    public string? SpecialInstructions { get; set; }
}
