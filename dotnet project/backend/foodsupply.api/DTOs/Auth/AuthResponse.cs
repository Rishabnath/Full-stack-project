namespace FoodSupply.Api.DTOs.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    // Keep this secret on the client and send it only to refresh or logout.
    public string RefreshToken { get; set; } = string.Empty;

    public Guid SessionId { get; set; }

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
