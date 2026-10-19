using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.Orders;

public sealed class CreateOrderRequest
{
    [Range(1, int.MaxValue)]
    public int RestaurantId { get; set; }

    [Range(1, int.MaxValue)]
    public int AddressId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string RecipientName { get; set; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 7)]
    public string RecipientPhoneNumber { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public List<CreateOrderItemRequest> Items { get; set; } = [];

    [StringLength(500)]
    public string? CustomerNote { get; set; }
}
