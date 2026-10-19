namespace FoodSupply.Api.Entities;

/// <summary>
/// One independently revocable login session for one user/device.
/// Only the SHA-256 hash of the refresh token is persisted.
/// </summary>
public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string RefreshTokenHash { get; set; } = string.Empty;

    public string DeviceName { get; set; } = "Unknown device";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public Guid? ReplacedBySessionId { get; set; }
}
