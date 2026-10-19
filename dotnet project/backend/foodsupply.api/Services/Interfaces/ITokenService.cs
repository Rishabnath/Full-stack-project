using FoodSupply.Api.Entities;
using FoodSupply.Api.Security;

namespace FoodSupply.Api.Services.Interfaces;

public interface ITokenService
{
    IssuedAccessToken CreateAccessToken(ApplicationUser user, Guid sessionId);

    IssuedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
