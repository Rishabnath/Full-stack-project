using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Security;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebUtilities;

namespace FoodSupply.Api.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
        ValidateOptions(_jwtOptions);
    }

    public IssuedAccessToken CreateAccessToken(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_jwtOptions.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("given_name", user.FirstName),
            new("family_name", user.LastName),
            new("role", user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new IssuedAccessToken(tokenValue, expiresAtUtc);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var refreshToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(64));
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        return new IssuedRefreshToken(
            refreshToken,
            HashRefreshToken(refreshToken),
            expiresAtUtc);
    }

    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hashBytes);
    }

    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("JWT issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT audience is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey) ||
            Encoding.UTF8.GetByteCount(options.SecretKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT secret key must be configured and contain at least 32 UTF-8 bytes. " +
                "Store it in .NET User Secrets or a deployment secret store.");
        }

        if (options.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT access-token lifetime must be greater than zero minutes.");
        }

        if (options.RefreshTokenDays <= 0)
        {
            throw new InvalidOperationException(
                "Refresh-token lifetime must be greater than zero days.");
        }
    }
}
