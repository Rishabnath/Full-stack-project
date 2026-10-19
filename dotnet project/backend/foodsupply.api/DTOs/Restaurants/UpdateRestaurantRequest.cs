using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.Restaurants;

public sealed class UpdateRestaurantRequest
{
    [Required(ErrorMessage = "Restaurant name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Restaurant address is required.")]
    [StringLength(250, MinimumLength = 5)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}
