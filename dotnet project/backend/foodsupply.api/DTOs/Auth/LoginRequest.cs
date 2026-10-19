using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(320, ErrorMessage = "Email cannot exceed 320 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(128, ErrorMessage = "Password cannot exceed 128 characters.")]
    public string Password { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Device name cannot exceed 100 characters.")]
    public string? DeviceName { get; set; }
}
