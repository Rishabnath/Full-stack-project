namespace FoodSupply.Api.DTOs.Auth;

public sealed class SessionResponse
{
    public Guid Id { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsCurrent { get; set; }
}
