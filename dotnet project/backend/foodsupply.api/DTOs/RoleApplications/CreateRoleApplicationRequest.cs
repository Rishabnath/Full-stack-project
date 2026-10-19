using System.ComponentModel.DataAnnotations;
using FoodSupply.Api.Entities;

namespace FoodSupply.Api.DTOs.RoleApplications;

public sealed class CreateRoleApplicationRequest
{
    [EnumDataType(typeof(UserRole), ErrorMessage = "Select a valid role.")]
    public UserRole RequestedRole { get; set; }

    [StringLength(150, ErrorMessage = "Business name cannot exceed 150 characters.")]
    public string? BusinessName { get; set; }

    [Required(ErrorMessage = "Application details are required.")]
    [StringLength(
        1500,
        MinimumLength = 20,
        ErrorMessage = "Application details must be between 20 and 1500 characters.")]
    public string ApplicationDetails { get; set; } = string.Empty;
}
