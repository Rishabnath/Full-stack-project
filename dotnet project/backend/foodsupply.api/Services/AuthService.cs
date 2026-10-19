using FoodSupply.Api.DTOs.Auth;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Security;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FoodSupply.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _userSessionRepository = userSessionRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new BadRequestException("Registration request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            throw new BadRequestException(
                "First name, last name, email, password, and password confirmation are required.");
        }

        if (!string.Equals(
                request.Password,
                request.ConfirmPassword,
                StringComparison.Ordinal))
        {
            throw new BadRequestException("Passwords do not match.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);

        if (await _userRepository.ExistsByEmailAsync(
                normalizedEmail,
                cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = NormalizeOptionalPhoneNumber(request.PhoneNumber),
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user, cancellationToken);

        // The session repository saves both the new user and their first session
        // through the same request-scoped DbContext.
        return await CreateAuthResponseAsync(
            user,
            request.DeviceName,
            cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new BadRequestException("Login request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("Email and password are required.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        // Use the same message for unknown, inactive, and wrong-password accounts.
        // This avoids revealing whether a particular email has an account.
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        return await CreateAuthResponseAsync(
            user,
            request.DeviceName,
            cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var currentTokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var currentSession = await _userSessionRepository.GetByRefreshTokenHashAsync(
            currentTokenHash,
            cancellationToken);
        var nowUtc = DateTime.UtcNow;

        if (currentSession is null ||
            currentSession.RevokedAtUtc.HasValue ||
            currentSession.ExpiresAtUtc <= nowUtc ||
            !currentSession.User.IsActive)
        {
            throw new UnauthorizedException(
                "Refresh token is invalid, expired, or revoked. Log in again.");
        }

        // Rotate on every refresh. The old token becomes unusable as the new
        // session is committed, so a stolen token cannot be reused indefinitely.
        var nextRefreshToken = _tokenService.CreateRefreshToken();
        var nextSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = currentSession.UserId,
            RefreshTokenHash = nextRefreshToken.Hash,
            DeviceName = currentSession.DeviceName,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nextRefreshToken.ExpiresAtUtc
        };
        var accessToken = _tokenService.CreateAccessToken(currentSession.User);

        var rotated = await _userSessionRepository.TryRotateAsync(
            currentSession.Id,
            currentTokenHash,
            nowUtc,
            nextSession,
            cancellationToken);

        if (!rotated)
        {
            throw new UnauthorizedException(
                "Refresh token has already been used or revoked. Log in again.");
        }

        return CreateAuthResponse(
            currentSession.User,
            nextSession.Id,
            accessToken,
            nextRefreshToken);
    }

    public async Task LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var refreshTokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        await _userSessionRepository.RevokeByRefreshTokenHashAsync(
            refreshTokenHash,
            DateTime.UtcNow,
            cancellationToken);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        ApplicationUser user,
        string? deviceName,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var accessToken = _tokenService.CreateAccessToken(user);
        var refreshToken = _tokenService.CreateRefreshToken();
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = refreshToken.Hash,
            DeviceName = NormalizeDeviceName(deviceName),
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        };

        await _userSessionRepository.AddAsync(session, cancellationToken);
        await _userSessionRepository.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, session.Id, accessToken, refreshToken);
    }

    private static AuthResponse CreateAuthResponse(
        ApplicationUser user,
        Guid sessionId,
        IssuedAccessToken accessToken,
        IssuedRefreshToken refreshToken)
    {

        return new AuthResponse
        {
            AccessToken = accessToken.Value,
            RefreshToken = refreshToken.Value,
            SessionId = sessionId,
            TokenType = "Bearer",
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    private static string NormalizeDeviceName(string? deviceName)
    {
        return string.IsNullOrWhiteSpace(deviceName)
            ? "Unknown device"
            : deviceName.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptionalPhoneNumber(string? phoneNumber)
    {
        return string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim();
    }
}
