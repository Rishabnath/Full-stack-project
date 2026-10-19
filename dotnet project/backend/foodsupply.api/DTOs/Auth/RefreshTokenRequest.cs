using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    [StringLength(512, ErrorMessage = "Refresh token is not valid.")]
    public string RefreshToken { get; set; } = string.Empty;
}
