using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.MenuItems;

public sealed class CreateMenuItemRequest
{
    [Required(ErrorMessage = "Menu item name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999.99")]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a valid category.")]
    public int CategoryId { get; set; }

    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}
