namespace FoodSupply.Api.Security;

public sealed record IssuedAccessToken(string Value, DateTime ExpiresAtUtc);
