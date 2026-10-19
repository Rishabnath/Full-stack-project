using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.Addresses;

public sealed class UpdateAddressRequest
{
    [Required]
    [StringLength(60, MinimumLength = 2)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [StringLength(250, MinimumLength = 5)]
    public string Line1 { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Line2 { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Country { get; set; } = string.Empty;
}
