namespace FoodSupply.Api.Security;

public sealed record IssuedRefreshToken(
    string Value,
    string Hash,
    DateTime ExpiresAtUtc);
